using System.Net;
using System.Text;
using System.Text.Json;
using Floudy.API.Services;
using Floudy.API.Storage;
using Floudy.API.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Floudy.API.Tests.Controllers;

public class FakeEmailService : EmailService
{
    public string? LastResetUrl { get; set; }

    public FakeEmailService(IConfiguration config) : base(config)
    {
    }

    public override void SendRecoveryEmail(string recipientEmail, string username, string resetUrl)
    {
        LastResetUrl = resetUrl;
    }
}

[Collection("Sequential")]
public class AuthControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;
    private readonly TokenService tokenService;
    private readonly FakeEmailService emailService;

    public AuthControllerTests()
    {
        GlobalIdManagerHelper.Reset();

        var userRepo = TestDataHelper.CreateEmptyUserRepository();
        var logRepo = new LogRepository(TestDataHelper.TestConnectionString);
        var userService = new UserService(userRepo);
        var logService = new LogService(logRepo, userService);
        tokenService = new TokenService();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SmtpSettings:Host"] = "localhost",
            ["SmtpSettings:Port"] = "25",
            ["SmtpSettings:Email"] = "test@example.com",
            ["GmailAppKey"] = "test-app-password"
        }).Build();

        emailService = new FakeEmailService(config);

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                ReplaceService<UserService>(services, userService);
                ReplaceService<LogService>(services, logService);
                ReplaceService<TokenService>(services, tokenService);
                ReplaceService<EmailService>(services, emailService);
            });
        });

        client = factory.CreateClient();
    }

    private static void ReplaceService<T>(IServiceCollection services, T instance) where T : class
    {
        var existing = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (existing != null) services.Remove(existing);
        services.AddSingleton(instance);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GlobalIdManagerHelper.Reset();
    }

    private StringContent Json(object body) => new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    // ── Registration ──────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidData_Returns200()
    {
        var res = await client.PostAsync("/api/auth/register", Json(new { username = "newuser", email = "new@example.com", password = "password1" }));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Register_MissingEmail_Returns400()
    {
        var res = await client.PostAsync("/api/auth/register", Json(new { username = "user2", email = "", password = "password1" }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("Email is required", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Register_InvalidEmailFormat_Returns400()
    {
        var res = await client.PostAsync("/api/auth/register", Json(new { username = "user3", email = "notanemail", password = "password1" }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("Invalid email format", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Register_DuplicateUsername_Returns400()
    {
        await client.PostAsync("/api/auth/register", Json(new { username = "dupuser", email = "dup1@example.com", password = "password1" }));
        var res = await client.PostAsync("/api/auth/register", Json(new { username = "dupuser", email = "dup2@example.com", password = "password1" }));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("Username already exists", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        await client.PostAsync("/api/auth/register", Json(new { username = "emailuser1", email = "shared@example.com", password = "password1" }));
        var res = await client.PostAsync("/api/auth/register", Json(new { username = "emailuser2", email = "shared@example.com", password = "password1" }));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("Email already exists", await res.Content.ReadAsStringAsync());
    }

    // ── Login ─────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithUsername_ReturnsToken()
    {
        var res = await client.PostAsync("/api/auth/login", Json(new { username = "admin", password = "admin" }));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.TryGetProperty("token", out var tokenProp));
        Assert.False(string.IsNullOrEmpty(tokenProp.GetString()));
    }

    [Fact]
    public async Task Login_WithEmail_ReturnsToken()
    {
        var res = await client.PostAsync("/api/auth/login", Json(new { username = "prooms.email@gmail.com", password = "admin" }));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.TryGetProperty("token", out var tokenProp));
        Assert.False(string.IsNullOrEmpty(tokenProp.GetString()));
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        var res = await client.PostAsync("/api/auth/login", Json(new { username = "admin", password = "wrong" }));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_AdminUser_ReturnsAdminRole()
    {
        var res = await client.PostAsync("/api/auth/login", Json(new { username = "admin", password = "admin" }));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.Equal("Admin", body.RootElement.GetProperty("role").GetString());
    }

    // ── Recovery Check ────────────────────────────────────────────

    [Fact]
    public async Task RecoveryCheck_ExistingUser_Returns200()
    {
        var res = await client.PostAsync("/api/auth/recovery/check", Json(new { usernameOrEmail = "admin" }));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task RecoveryCheck_ExistingEmail_Returns200()
    {
        var res = await client.PostAsync("/api/auth/recovery/check", Json(new { usernameOrEmail = "prooms.email@gmail.com" }));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task RecoveryCheck_NonExistentUser_Returns400()
    {
        var res = await client.PostAsync("/api/auth/recovery/check", Json(new { usernameOrEmail = "ghost" }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ── Password Reset Flow Integration ───────────────────────────

    [Fact]
    public async Task RecoverySend_And_ResetPassword_FullFlow()
    {
        // 1. Send recovery email
        var sendRes = await client.PostAsync("/api/auth/recovery/send", Json(new { usernameOrEmail = "admin" }));
        Assert.Equal(HttpStatusCode.OK, sendRes.StatusCode);
        Assert.NotNull(emailService.LastResetUrl);

        // 2. Extract reset token from generated URL
        var token = emailService.LastResetUrl!.Split("reset=")[1];
        Assert.NotEmpty(token);

        // 3. Validate token
        var valRes = await client.GetAsync($"/api/auth/recovery/validate?token={token}");
        Assert.Equal(HttpStatusCode.OK, valRes.StatusCode);

        // 4. Validate invalid token (401)
        var valInvalidRes = await client.GetAsync("/api/auth/recovery/validate?token=non_existent_token");
        Assert.Equal(HttpStatusCode.Unauthorized, valInvalidRes.StatusCode);

        // 5. Reset password with new value
        var resetRes = await client.PostAsync("/api/auth/recovery/reset", Json(new { token = token, password = "new_admin_password123" }));
        Assert.Equal(HttpStatusCode.OK, resetRes.StatusCode);

        // 6. Verify logging in with old password fails
        var loginOldRes = await client.PostAsync("/api/auth/login", Json(new { username = "admin", password = "admin" }));
        Assert.Equal(HttpStatusCode.Unauthorized, loginOldRes.StatusCode);

        // 7. Verify logging in with new password succeeds
        var loginNewRes = await client.PostAsync("/api/auth/login", Json(new { username = "admin", password = "new_admin_password123" }));
        Assert.Equal(HttpStatusCode.OK, loginNewRes.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_Returns410()
    {
        // 1. Send recovery email
        var sendRes = await client.PostAsync("/api/auth/recovery/send", Json(new { usernameOrEmail = "admin" }));
        Assert.Equal(HttpStatusCode.OK, sendRes.StatusCode);

        var token = emailService.LastResetUrl!.Split("reset=")[1];

        // 2. Access/Modify token in TokenService to simulate expiration
        tokenService.ValidateResetToken(token, out var resetToken);
        Assert.NotNull(resetToken);
        resetToken!.ExpiresAt = DateTime.UtcNow.AddMinutes(-10); // set to past

        // 3. Validate token now returns 410 Gone
        var valRes = await client.GetAsync($"/api/auth/recovery/validate?token={token}");
        Assert.Equal(HttpStatusCode.Gone, valRes.StatusCode);

        // 4. Reset password attempt with expired token returns 410 Gone
        var resetRes = await client.PostAsync("/api/auth/recovery/reset", Json(new { token = token, password = "some_new_password" }));
        Assert.Equal(HttpStatusCode.Gone, resetRes.StatusCode);
    }

    // ── Logout ────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_InvalidatesToken()
    {
        // Login first
        var loginRes = await client.PostAsync("/api/auth/login", Json(new { username = "admin", password = "admin" }));
        var loginBody = JsonDocument.Parse(await loginRes.Content.ReadAsStringAsync());
        var token = loginBody.RootElement.GetProperty("token").GetString();

        // Logout
        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutReq.Headers.Add("Authorization", $"Bearer {token}");
        var logoutRes = await client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.OK, logoutRes.StatusCode);

        // Try using the token - should be rejected
        var filesReq = new HttpRequestMessage(HttpMethod.Get, "/api/files/page/1");
        filesReq.Headers.Add("Authorization", $"Bearer {token}");
        var filesRes = await client.SendAsync(filesReq);
        Assert.Equal(HttpStatusCode.Unauthorized, filesRes.StatusCode);
    }
}
