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
public class FileControllerTests : IAsyncLifetime, IDisposable
{
    private const long TestUserId = 1;

    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;
    private readonly FileService file_service;
    private readonly TokenService token_service;
    private readonly UserService user_service;

    public FileControllerTests()
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
                ReplaceService(services, new LogService(new LogRepository(TestDataHelper.TestConnectionString), user_service));
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

    private void SeedFile(string name = "sample.txt", string type = "text/plain", long size = 42, long ms = 1_700_000_000_000L, byte[]? raw = null)
    {
        file_service.UploadFromMetadata(TestDataHelper.ValidMetadata(raw: raw, 
                                                                     size: size, 
                                                                     uploaded_ms: ms, 
                                                                     name: name, 
                                                                     type: type), TestUserId);
    }

    private static MultipartFormDataContent BuildMultipartContent(byte[] file_bytes, string file_name, string metadata_json)
    {
        var content = new MultipartFormDataContent();
        var file_part = new ByteArrayContent(file_bytes);
        file_part.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

        content.Add(file_part, "file", file_name);
        content.Add(new StringContent(metadata_json, Encoding.UTF8), "metadata");

        return content;
    }

    [Fact]
    public async Task GetPage_EmptyRepository_ReturnsOkWithZeroFiles()
    {
        var response = await client.GetAsync("/api/files/page/1");
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, doc.GetProperty("files").GetArrayLength());
    }

    [Fact]
    public async Task GetPage_PageOne_ReturnsSeededFiles()
    {
        SeedFile("alpha.txt");
        SeedFile("beta.txt");

        var response = await client.GetAsync("/api/files/page/1");
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, doc.GetProperty("files").GetArrayLength());
    }

    [Fact]
    public async Task GetPage_ResponseIncludesTotalPagesField()
    {
        for (var i = 0; i < 11; i++) SeedFile($"f{i}.txt");

        var response = await client.GetAsync("/api/files/page/1");
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(2, doc.GetProperty("total_pages").GetInt32());
    }

    [Fact]
    public async Task GetPage_PageTwoWith11Files_ReturnsOneFile()
    {
        for (var i = 0; i < 11; i++) SeedFile($"f{i}.txt");

        var response = await client.GetAsync("/api/files/page/2");
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(1, doc.GetProperty("files").GetArrayLength());
    }

    [Fact]
    public async Task GetPage_PageZero_ReturnsOkWithEmptyFilesList()
    {
        SeedFile();

        var response = await client.GetAsync("/api/files/page/0");
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, doc.GetProperty("files").GetArrayLength());
    }

    [Fact]
    public async Task GetPage_FileEntry_ContainsAllExpectedFields()
    {
        SeedFile("hello.txt", raw: [0x48, 0x69]);

        var response = await client.GetAsync("/api/files/page/1");
        var file = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                               .RootElement.GetProperty("files")[0];

        Assert.True(file.TryGetProperty("id", out _));
        Assert.True(file.TryGetProperty("name", out _));
        Assert.True(file.TryGetProperty("size", out _));
        Assert.True(file.TryGetProperty("type", out _));
        Assert.True(file.TryGetProperty("uploaded", out _));
        Assert.True(file.TryGetProperty("base64", out _));
    }

    [Fact]
    public async Task GetPage_FileEntry_Base64MatchesOriginalContent()
    {
        byte[] raw = [0x41, 0x42, 0x43];
        SeedFile(raw: raw);

        var response = await client.GetAsync("/api/files/page/1");
        var b64 = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                              .RootElement
                              .GetProperty("files")[0]
                              .GetProperty("base64")
                              .GetString()!;

        Assert.Equal(Convert.ToBase64String(raw), b64);
    }

    [Fact]
    public async Task GetRecent_EmptyRepository_ReturnsOkWithEmptyList()
    {
        var response = await client.GetAsync("/api/files/recent");
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, doc.GetProperty("files").GetArrayLength());
    }

    [Fact]
    public async Task GetRecent_DefaultCount_CappsAtTen()
    {
        for (var i = 0; i < 15; i++) SeedFile($"f{i}.txt");

        var response = await client.GetAsync("/api/files/recent");
        var count = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                                .RootElement
                                .GetProperty("files")
                                .GetArrayLength();

        Assert.Equal(10, count);
    }

    [Fact]
    public async Task GetRecent_CustomCountQueryParam_LimitsResults()
    {
        for (var i = 0; i < 5; i++) SeedFile($"f{i}.txt");

        var response = await client.GetAsync("/api/files/recent?count=2");
        var count = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                                .RootElement
                                .GetProperty("files")
                                .GetArrayLength();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetRecent_ResultsAreOrderedByUploadDateDescending()
    {
        var old_ms = new DateTimeOffset(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        var new_ms = new DateTimeOffset(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

        SeedFile("older.txt", ms: old_ms);
        SeedFile("newer.txt", ms: new_ms);

        var response = await client.GetAsync("/api/files/recent?count=2");
        var files = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                                .RootElement
                                .GetProperty("files");

        Assert.True(files[0].GetProperty("uploaded").GetDateTime() >= files[1].GetProperty("uploaded").GetDateTime());
    }

    [Fact]
    public async Task Post_ValidPayload_Returns201Created()
    {
        var metadata_json = JsonSerializer.Serialize(new
        {
            size     = 3,
            uploaded = 1_700_000_000_000L,
            name     = "upload.txt",
            type     = "text/plain"
        });

        var response = await client.PostAsync("/api/files", BuildMultipartContent([0x01, 0x02, 0x03], "upload.txt", metadata_json));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidPayload_PersistsFileInRepository()
    {
        var metadata_json = JsonSerializer.Serialize(new
        {
            size     = 2,
            uploaded = 1_700_000_000_000L,
            name     = "persisted.txt",
            type     = "text/plain"
        });

        await client.PostAsync("/api/files", BuildMultipartContent([0xAA, 0xBB], "persisted.txt", metadata_json));

        Assert.Equal(1, file_service.Files.CountByUserId(TestUserId));
    }

    [Fact]
    public async Task Post_NegativeSize_Returns422UnprocessableEntity()
    {
        var metadata_json = JsonSerializer.Serialize(new
        {
            size     = -1,
            uploaded = 1_700_000_000_000L,
            name     = "bad.txt",
            type     = "text/plain"
        });

        var response = await client.PostAsync("/api/files", BuildMultipartContent([0x00], "bad.txt", metadata_json));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyName_Returns422UnprocessableEntity()
    {
        var metadata_json = JsonSerializer.Serialize(new
        {
            size     = 1,
            uploaded = 1_700_000_000_000L,
            name     = "",
            type     = "text/plain"
        });

        var response = await client.PostAsync("/api/files", BuildMultipartContent([0x00], "unnamed", metadata_json));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidPayload_DoesNotAddFileToRepository()
    {
        var metadata_json = JsonSerializer.Serialize(new
        {
            size     = -1,
            uploaded = 1_700_000_000_000L,
            name     = "bad.txt",
            type     = "text/plain"
        });

        await client.PostAsync("/api/files", BuildMultipartContent([0x00], "bad.txt", metadata_json));
        Assert.Equal(0, file_service.Files.CountByUserId(TestUserId));
    }


    [Fact]
    public async Task Put_ValidIdAndName_RenamesFile()
    {
        SeedFile("original.txt");
        var id = file_service.Files.GetByUserId(TestUserId).Single().ID;
        await client.PutAsync($"/api/files/rename/{id}?name=renamed.txt", null);

        Assert.Equal("renamed.txt", file_service.Files.GetByUserId(TestUserId).Single().Name);
    }

    [Fact]
    public async Task Put_NonExistentId_Returns400BadRequest()
    {
        var response = await client.PutAsync("/api/files/rename/9999?name=name.txt", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_EmptyName_Returns400BadRequest()
    {
        SeedFile("original.txt");
        var id = file_service.Files.GetByUserId(TestUserId).Single().ID;
        var response = await client.PutAsync($"/api/files/rename/{id}?name=", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingFile_RemovesItFromRepository()
    {
        SeedFile();
        var id = file_service.Files.GetByUserId(TestUserId).Single().ID;
        await client.DeleteAsync($"/api/files/delete/{id}");

        Assert.Equal(0, file_service.Files.CountByUserId(TestUserId));
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404NotFound()
    {
        var response = await client.DeleteAsync("/api/files/delete/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SameIdTwice_SecondCallReturns404()
    {
        SeedFile();
        var id = file_service.Files.GetByUserId(TestUserId).Single().ID;
        await client.DeleteAsync($"/api/files/delete/{id}");

        var second_response = await client.DeleteAsync($"/api/files/delete/{id}");

        Assert.Equal(HttpStatusCode.NotFound, second_response.StatusCode);
    }
}