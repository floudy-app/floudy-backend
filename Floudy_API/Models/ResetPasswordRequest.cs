namespace Floudy.API.Models
{
    public record ResetPasswordRequest(string Token, string Password);
}
