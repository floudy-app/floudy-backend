namespace Floudy.API.Storage.Entities
{
    public class UserEntity
    {
        public long ID { get; set; }

        public string Username { get; set; } = null!;
        
        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;

        public long RoleId { get; set; }

        public bool IsBlocked { get; set; }

        public RoleEntity Role { get; set; } = null!;
    }
}
