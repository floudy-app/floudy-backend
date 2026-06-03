using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Floudy.API.Services;
using Floudy.API.Storage;
using Floudy.API.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Floudy.API.Tests.Controllers;

[Collection("Sequential")]
public class StatsControllerTests : IAsyncLifetime, IDisposable
{
    private const long TestUserId = 1;

    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;
    private readonly FileService file_service;
    private readonly TokenService token_service;
    private readonly UserService user_service;

    public StatsControllerTests()
    {
        GlobalIdManagerHelper.Reset();

        file_service = new FileService(TestDataHelper.CreateEmptyRepository());
        user_service = new UserService(TestDataHelper.CreateEmptyUserRepository());
        token_service = new TokenService();

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                ReplaceService(services, file_service);
                ReplaceService(services, user_service);
                ReplaceService(services, token_service);
            });
        });
        client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        var loginRes = await client.PostAsync("/api/auth/login", Json(new { username = "admin", password = "admin" }));
        loginRes.EnsureSuccessStatusCode();
        var loginBody = JsonDocument.Parse(await loginRes.Content.ReadAsStringAsync());
        var token = loginBody.RootElement.GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static void ReplaceService<T>(IServiceCollection services, T instance) where T : class
    {
        var existing = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (existing != null) services.Remove(existing);
        services.AddSingleton(instance);
    }

    private static StringContent Json(object body) => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GlobalIdManagerHelper.Reset();
    }

    private void SeedFile(
        string name = "f.txt",
        string type = "text/plain",
        long ms = 1_700_000_000_000L)
    {
        file_service.UploadFromMetadata(TestDataHelper.ValidMetadata(name: name, type: type, uploaded_ms: ms), TestUserId);
    }

    [Fact]
    public async Task GetByType_EmptyRepository_ReturnsOkWithEmptyEntries()
    {
        var response = await client.GetAsync("/api/stats/type");
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, doc.GetProperty("entries").GetArrayLength());
    }

    [Fact]
    public async Task GetByType_SingleMimeCategory_ReturnsOneEntry()
    {
        SeedFile("a.txt", "text/plain");
        SeedFile("b.txt", "text/html");

        var response = await client.GetAsync("/api/stats/type");
        var entries = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                                  .RootElement.GetProperty("entries");

        Assert.Equal(1, entries.GetArrayLength());
        Assert.Equal("text", entries[0].GetProperty("type").GetString());
        Assert.Equal(2, entries[0].GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task GetByType_MultipleMimeCategories_ReturnsOneEntryPerCategory()
    {
        SeedFile("a.txt", "text/plain");
        SeedFile("b.png", "image/png");
        SeedFile("c.jpg", "image/jpeg");
        SeedFile("d.json", "application/json");

        var response = await client.GetAsync("/api/stats/type");
        var entries = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                                  .RootElement
                                  .GetProperty("entries");

        Assert.Equal(3, entries.GetArrayLength());
    }

    [Fact]
    public async Task GetByType_EntryCountMatchesFilesInCategory()
    {
        SeedFile("a.png", "image/png");
        SeedFile("b.png", "image/png");
        SeedFile("c.jpg", "image/jpeg");

        var response = await client.GetAsync("/api/stats/type");
        var entries = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                                  .RootElement.GetProperty("entries");

        var image_entry = Enumerable.Range(0, entries.GetArrayLength())
                                    .Select(i => entries[i])
                                    .Single(e => e.GetProperty("type").GetString() == "image");

        Assert.Equal(3, image_entry.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task GetByUpload_EmptyRepository_ReturnsOkWithEmptyEntries()
    {
        var response = await client.GetAsync("/api/stats/upload");
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, doc.GetProperty("entries").GetArrayLength());
    }

    [Fact]
    public async Task GetByUpload_FilesInSameMonth_ReturnsSingleEntry()
    {
        var jan_a = new DateTimeOffset(new DateTime(2024, 1,  5, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        var jan_b = new DateTimeOffset(new DateTime(2024, 1, 20, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

        SeedFile("a.txt", ms: jan_a);
        SeedFile("b.txt", ms: jan_b);

        var response = await client.GetAsync("/api/stats/upload");
        var entries = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                                  .RootElement
                                  .GetProperty("entries");

        Assert.Equal(1, entries.GetArrayLength());
        Assert.Equal(2, entries[0].GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task GetByUpload_FilesInDifferentMonths_ReturnsOneEntryPerMonth()
    {
        var jan = new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        var feb = new DateTimeOffset(new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        var mar = new DateTimeOffset(new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

        SeedFile("a.txt", ms: jan);
        SeedFile("b.txt", ms: feb);
        SeedFile("c.txt", ms: mar);

        var response = await client.GetAsync("/api/stats/upload");
        var entries = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                                  .RootElement
                                  .GetProperty("entries");

        Assert.Equal(3, entries.GetArrayLength());
    }

    [Fact]
    public async Task GetByUpload_DateFieldFormattedAsYearSlashMonth()
    {
        var ms = new DateTimeOffset(new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        SeedFile(ms: ms);

        var response = await client.GetAsync("/api/stats/upload");
        var date = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                               .RootElement.GetProperty("entries")[0]
                               .GetProperty("date")
                               .GetString()!;

        Assert.Equal("2024/03", date);
    }

    [Fact]
    public async Task GetByUpload_MonthIsTwoDigitsWithLeadingZero()
    {
        var ms = new DateTimeOffset(new DateTime(2024, 9, 1, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        SeedFile(ms: ms);

        var response = await client.GetAsync("/api/stats/upload");
        var date = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                               .RootElement
                               .GetProperty("entries")[0]
                               .GetProperty("date")
                               .GetString()!;

        Assert.Equal("2024/09", date);
    }
}
