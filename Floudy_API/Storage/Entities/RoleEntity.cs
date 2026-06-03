namespace Floudy.API.Storage.Entities
{
    public class RoleEntity
    {
        public long ID { get; set; }
        public string Name { get; set; } = null!;
        public ICollection<UserEntity> Users { get; set; } = new List<UserEntity>();
    }
}
