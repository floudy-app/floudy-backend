namespace Floudy.API.Models
{
    public record RawFileMetadata(string Name);

    public class RawFile
    (
        long id, 
        string name, 
        long byte_size,
        string type,
        DateTime upload_date,
        byte[] content)
    {
        public long ID { get; } = id;

        public string Name { get; set; } = name;
        
        public long ByteSize { get; } = byte_size;
        
        public string Type { get; } = type;
        
        public DateTime UploadDate { get; } = upload_date;
        
        public byte[] Content { get; } = content;
    }
}