using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace SchoolAdmission.IntegrationTests;

public sealed class ClassesApiTests : IClassFixture<IntegrationTestFactory>, IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ClassesApiTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task POST_classes_ValidRequest_Returns201WithClass()
    {
        var payload = new { name = "Class 6 - Morning Shift", totalSeats = 120 };

        var response = await _client.PostAsJsonAsync("/classes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("Class 6 - Morning Shift");
        body.GetProperty("totalSeats").GetInt32().Should().Be(120);
        body.GetProperty("id").GetString().Should().StartWith("c_");
    }

    [Fact]
    public async Task POST_classes_EmptyName_Returns422()
    {
        var payload = new { name = "", totalSeats = 10 };
        var response = await _client.PostAsJsonAsync("/classes", payload);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task POST_classes_ZeroSeats_Returns422()
    {
        var payload = new { name = "Class 7", totalSeats = 0 };
        var response = await _client.PostAsJsonAsync("/classes", payload);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GET_classes_ReturnsListWithSeatCounts()
    {
        await _client.PostAsJsonAsync("/classes", new { name = "Class A", totalSeats = 50 });
        await _client.PostAsJsonAsync("/classes", new { name = "Class B", totalSeats = 30 });

        var response = await _client.GetAsync("/classes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<JsonElement>();
        list.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GET_classes_classId_ExistingClass_Returns200()
    {
        var create = await _client.PostAsJsonAsync("/classes", new { name = "Class C", totalSeats = 60 });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var classId = created.GetProperty("id").GetString()!;

        var response = await _client.GetAsync($"/classes/{classId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalSeats").GetInt32().Should().Be(60);
        body.GetProperty("availableSeats").GetInt32().Should().Be(60);
    }

    [Fact]
    public async Task GET_classes_classId_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/classes/c_nonexistent");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
