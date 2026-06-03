using Floudy.API.Services;
using Floudy.API.Storage.Entities;
using Xunit;

namespace Floudy.API.Tests.Services;

public class TokenServiceTests
{
    private TokenService CreateService() => new TokenService();

    private UserEntity CreateUser(string role = "User") => new UserEntity
    {
        ID = 1,
        Username = "testuser",
        Email = "test@example.com",
        Password = "password",
        RoleId = role == "Admin" ? 1 : 2,
        IsBlocked = false,
        Role = new RoleEntity
        {
            ID = role == "Admin" ? 1 : 2,
            Name = role
        }
    };

    [Fact]
    public void GenerateToken_ReturnsNonNullToken()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateToken(user);

        Assert.NotNull(token);
        Assert.NotEmpty(token.Token);
        Assert.Equal("testuser", token.Username);
        Assert.Equal(1, token.UserId);
    }

    [Fact]
    public void GenerateToken_SetsRoleFromUser()
    {
        var service = CreateService();
        var user = CreateUser("Admin");

        var token = service.GenerateToken(user);

        Assert.Equal("Admin", token.Role);
    }

    [Fact]
    public void ValidateAndSlideToken_ValidToken_ReturnsToken()
    {
        var service = CreateService();
        var user = CreateUser();
        var generated = service.GenerateToken(user);

        var result = service.ValidateAndSlideToken(generated.Token);

        Assert.NotNull(result);
        Assert.Equal(generated.Token, result!.Token);
    }

    [Fact]
    public void ValidateAndSlideToken_InvalidToken_ReturnsNull()
    {
        var service = CreateService();

        var result = service.ValidateAndSlideToken("invalid-token-string");

        Assert.Null(result);
    }

    [Fact]
    public void ValidateAndSlideToken_NullOrEmpty_ReturnsNull()
    {
        var service = CreateService();

        Assert.Null(service.ValidateAndSlideToken(null!));
        Assert.Null(service.ValidateAndSlideToken(""));
    }

    [Fact]
    public void InvalidateToken_RemovesToken()
    {
        var service = CreateService();
        var user = CreateUser();
        var generated = service.GenerateToken(user);

        service.InvalidateToken(generated.Token);

        var result = service.ValidateAndSlideToken(generated.Token);
        Assert.Null(result);
    }

    [Fact]
    public void InvalidateToken_NoOpForNullOrEmpty()
    {
        var service = CreateService();

        service.InvalidateToken(null!);
        service.InvalidateToken("");
    }

    [Fact]
    public void MultipleTokens_AreIndependent()
    {
        var service = CreateService();
        var user = CreateUser();

        var token1 = service.GenerateToken(user);
        var token2 = service.GenerateToken(user);

        Assert.NotEqual(token1.Token, token2.Token);

        service.InvalidateToken(token1.Token);

        Assert.Null(service.ValidateAndSlideToken(token1.Token));
        Assert.NotNull(service.ValidateAndSlideToken(token2.Token));
    }
}
