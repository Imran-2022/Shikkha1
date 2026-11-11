using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace SchoolAdmission.IntegrationTests;

/// <summary>
/// Concurrency tests: fires many parallel hold requests and verifies
/// that held + confirmed seats never exceed totalSeats.
/// </summary>
public sealed class ConcurrencyTests : IClassFixture<IntegrationTestFactory>, IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory;

    public ConcurrencyTests(IntegrationTestFactory factory)
        => _factory = factory;

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PlaceHold_ManyParallelRequests_NeverExceedsTotalSeats()
    {
        const int totalSeats = 5;
        const int parallelRequests = 50;

        // Create class
        var adminClient = _factory.CreateClient();
        var createResp = await adminClient.PostAsJsonAsync("/classes",
            new { name = "Concurrency Test Class", totalSeats });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var classDoc = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var classId = classDoc.GetProperty("id").GetString()!;

        // Fire parallelRequests concurrent hold requests with unique student IDs
        var tasks = Enumerable.Range(0, parallelRequests).Select(i =>
        {
            var client = _factory.CreateClient();
            var studentId = $"{i:D17}"; // 17-digit student ID
            var req = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
            req.Headers.Add("X-Student-Id", studentId);
            return client.SendAsync(req);
        });

        var responses = await Task.WhenAll(tasks);

        int created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        int conflicts = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        // Only totalSeats requests should succeed
        created.Should().Be(totalSeats,
            because: "only as many holds as there are seats should be granted");

        // All remaining should be 409 Conflict (no seats)
        conflicts.Should().Be(parallelRequests - totalSeats,
            because: "all requests beyond seat capacity should be rejected");

        // Verify via API that seat counts are consistent
        var statusResp = await adminClient.GetAsync($"/classes/{classId}");
        var statusDoc = await statusResp.Content.ReadFromJsonAsync<JsonElement>();
        var heldSeats = statusDoc.GetProperty("heldSeats").GetInt32();
        var confirmedSeats = statusDoc.GetProperty("confirmedSeats").GetInt32();

        (heldSeats + confirmedSeats).Should().BeLessThanOrEqualTo(totalSeats,
            because: "held + confirmed must never exceed total seats");
    }

    [Fact]
    public async Task ConfirmHold_CalledMultipleTimes_ConfirmedSeatsIncrementOnlyOnce()
    {
        // Create a class
        var client = _factory.CreateClient();
        var createResp = await client.PostAsJsonAsync("/classes",
            new { name = "Idempotent Confirm Class", totalSeats = 10 });
        var classDoc = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var classId = classDoc.GetProperty("id").GetString()!;

        const string studentId = "11111111111111111";

        // Place hold
        var holdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        holdReq.Headers.Add("X-Student-Id", studentId);
        var holdResp = await client.SendAsync(holdReq);
        holdResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var holdDoc = await holdResp.Content.ReadFromJsonAsync<JsonElement>();
        var holdId = holdDoc.GetProperty("holdId").GetString()!;

        // Confirm same hold 5 times in parallel
        var confirmTasks = Enumerable.Range(0, 5).Select(_ =>
        {
            var c = _factory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdId}/confirm");
            req.Headers.Add("X-Student-Id", studentId);
            return c.SendAsync(req);
        });
        var confirmResponses = await Task.WhenAll(confirmTasks);

        // All should succeed (idempotent)
        confirmResponses.Should().AllSatisfy(r =>
            r.StatusCode.Should().Be(HttpStatusCode.OK));

        // But confirmedSeats should still be 1
        var statusResp = await client.GetAsync($"/classes/{classId}");
        var statusDoc = await statusResp.Content.ReadFromJsonAsync<JsonElement>();
        statusDoc.GetProperty("confirmedSeats").GetInt32().Should().Be(1);
    }
}
