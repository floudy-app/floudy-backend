namespace Floudy.API.Storage.Entities
{
    public class RawFileEntity
    {
        public long ID { get; set; }

        public string Name { get; set; } = null!;

        public string Type { get; set; } = null!;

        public DateTime UploadDate { get; set; }

        public byte[] Content { get; set; } = null!;

        public long UserId { get; set; }

        public UserEntity User { get; set; } = null!;
    }
}
