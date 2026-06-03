namespace Floudy.API.Models
{
    public class ResetToken
    {
        public string Token { get; set; } = null!;

        public long UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }
    }

    public class UserToken
    {
        public string Token { get; set; } = null!;

        public long UserId { get; set; }

        public string Username { get; set; } = null!;

        public string Role { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }
    }
}
