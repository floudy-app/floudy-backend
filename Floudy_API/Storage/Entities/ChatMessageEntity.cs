using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Floudy.API.Storage.Entities
{
    public class ChatMessageEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string Username { get; set; } = null!;

        public string Text { get; set; } = null!;

        public DateTime Timestamp { get; set; }
    }
}
