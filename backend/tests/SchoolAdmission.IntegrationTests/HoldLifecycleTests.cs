using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Infrastructure.Persistence;
using SchoolAdmission.Infrastructure.Services;
using Xunit;

namespace SchoolAdmission.IntegrationTests;

public sealed class HoldLifecycleTests : IClassFixture<IntegrationTestFactory>, IAsyncLifetime
{
    private const string StudentId = "22222222222222222";
    private readonly IntegrationTestFactory _factory;

    public HoldLifecycleTests(IntegrationTestFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<string> CreateClassAsync(HttpClient client, string name, int seats)
    {
        var resp = await client.PostAsJsonAsync("/classes", new { name, totalSeats = seats });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var doc = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return doc.GetProperty("id").GetString()!;
    }

    private static HttpRequestMessage Request(HttpMethod method, string url, string? studentId)
    {
        var req = new HttpRequestMessage(method, url);
        if (studentId is not null)
            req.Headers.Add("X-Student-Id", studentId);
        return req;
    }

    [Fact]
    public async Task GET_me_holds_MissingHeader_Returns422()
    {
        var client = _factory.CreateClient();
        var response = await client.SendAsync(Request(HttpMethod.Get, "/me/holds", null));
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GET_me_holds_MalformedHeader_Returns422()
    {
        var client = _factory.CreateClient();
        var response = await client.SendAsync(Request(HttpMethod.Get, "/me/holds", "123"));
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task POST_holds_MissingHeader_Returns422()
    {
        var client = _factory.CreateClient();
        var classId = await CreateClassAsync(client, "Header Test Class", 3);

        var response = await client.SendAsync(
            Request(HttpMethod.Post, $"/classes/{classId}/holds", null));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PlaceHold_SameStudentTwice_SecondReturns409()
    {
        var client = _factory.CreateClient();
        var classId = await CreateClassAsync(client, "Duplicate Hold Class", 5);

        var first = await client.SendAsync(Request(HttpMethod.Post, $"/classes/{classId}/holds", StudentId));
        var second = await client.SendAsync(Request(HttpMethod.Post, $"/classes/{classId}/holds", StudentId));

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PlaceHold_ThenConfirm_ThenCancel_Returns409AndKeepsSeat()
    {
        var client = _factory.CreateClient();
        var classId = await CreateClassAsync(client, "Confirm Then Cancel Class", 2);

        var place = await client.SendAsync(Request(HttpMethod.Post, $"/classes/{classId}/holds", StudentId));
        var holdId = (await place.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("holdId").GetString()!;

        var confirm = await client.SendAsync(Request(HttpMethod.Post, $"/holds/{holdId}/confirm", StudentId));
        confirm.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancel = await client.SendAsync(Request(HttpMethod.Delete, $"/holds/{holdId}", StudentId));
        cancel.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var status = await (await client.GetAsync($"/classes/{classId}")).Content.ReadFromJsonAsync<JsonElement>();
        status.GetProperty("confirmedSeats").GetInt32().Should().Be(1);
        status.GetProperty("availableSeats").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Confirm_ByDifferentStudent_Returns403()
    {
        var client = _factory.CreateClient();
        var classId = await CreateClassAsync(client, "Ownership Class", 2);

        var place = await client.SendAsync(Request(HttpMethod.Post, $"/classes/{classId}/holds", StudentId));
        var holdId = (await place.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("holdId").GetString()!;

        var confirm = await client.SendAsync(
            Request(HttpMethod.Post, $"/holds/{holdId}/confirm", "33333333333333333"));

        confirm.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExpiredHold_FreesSeat_AndSweeperWritesExpiredStatusToDatabase()
    {
        // 1-second holds so the test does not have to wait ten minutes.
        using var shortHoldFactory = _factory.WithWebHostBuilder(b =>
            b.UseSetting("HOLD_DURATION_SECONDS", "1"));
        var client = shortHoldFactory.CreateClient();

        var classId = await CreateClassAsync(client, "Expiry Class", 1);

        var place = await client.SendAsync(Request(HttpMethod.Post, $"/classes/{classId}/holds", StudentId));
        place.StatusCode.Should().Be(HttpStatusCode.Created);
        var holdId = (await place.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("holdId").GetString()!;

        var whileHeld = await (await client.GetAsync($"/classes/{classId}")).Content.ReadFromJsonAsync<JsonElement>();
        whileHeld.GetProperty("availableSeats").GetInt32().Should().Be(0);

        await Task.Delay(TimeSpan.FromSeconds(2.5));

        // Live evaluation: seat is back and API reports EXPIRED even before the sweeper ran
        var afterExpiry = await (await client.GetAsync($"/classes/{classId}")).Content.ReadFromJsonAsync<JsonElement>();
        afterExpiry.GetProperty("availableSeats").GetInt32().Should().Be(1);
        afterExpiry.GetProperty("heldSeats").GetInt32().Should().Be(0);

        var holdStatus = await (await client.GetAsync($"/holds/{holdId}")).Content.ReadFromJsonAsync<JsonElement>();
        holdStatus.GetProperty("status").GetString().Should().Be("EXPIRED");

        // Sweeper persists the status
        using (var scope = shortHoldFactory.Services.CreateScope())
        {
            var sweeper = scope.ServiceProvider.GetRequiredService<HoldExpirySweeper>();
            await sweeper.SweepAsync();
        }

        using (var scope = shortHoldFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.Holds.AsNoTracking().SingleAsync(h => h.Id == holdId);
            stored.Status.Should().Be(HoldStatus.Expired);
        }

        // Confirming an expired hold is rejected, and the freed seat can be taken by someone else
        var confirm = await client.SendAsync(Request(HttpMethod.Post, $"/holds/{holdId}/confirm", StudentId));
        confirm.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var other = await client.SendAsync(
            Request(HttpMethod.Post, $"/classes/{classId}/holds", "44444444444444444"));
        other.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
