using System.Text.Json;
using Floudy.API.Models;
using Floudy.API.Storage;

namespace Floudy.API.Tests.Helpers;

internal static class TestDataHelper
{
    internal static string TestConnectionString => $"Server=DESKTOP-PROOM\\SQLEXPRESS;Database=Test_FloubyDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    internal static FileRepository CreateEmptyRepository()
    {
        using (var ctx = new AppDbContext(TestConnectionString))
        {
            ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();
        }

        return new FileRepository(TestConnectionString);
    }

    internal static UserRepository CreateEmptyUserRepository()
    {
        using (var ctx = new AppDbContext(TestConnectionString))
        {
            ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();
        }

        return new UserRepository(TestConnectionString);
    }

    internal static JsonElement ToJsonElement<T>(T value) => JsonDocument.Parse(JsonSerializer.Serialize(value)).RootElement.Clone();

    internal static Dictionary<string, object> ValidMetadata
    (
        byte[]? raw = null,
        long size = 42,
        long uploaded_ms = 1_700_000_000_000L,
        string name = "test.txt",
        string type = "text/plain"
    ) 
    => new()
    {
        ["raw"] = (object)(raw ?? [0x01, 0x02, 0x03]),
        ["size"] = ToJsonElement(size),
        ["uploaded"] = ToJsonElement(uploaded_ms),
        ["name"] = name,
        ["type"] = type
    };

    internal static RawFile CreateFile
    (
        long id = 1,
        string name = "test.txt",
        long byte_size = 42,
        string type = "text/plain",
        DateTime upload_date = default,
        byte[]? content = null
    ) 
    => new
    (
        id,
        name,
        byte_size,
        type,
        upload_date == default ? new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) : upload_date,
        content ?? [0x01, 0x02, 0x03]
    );
}