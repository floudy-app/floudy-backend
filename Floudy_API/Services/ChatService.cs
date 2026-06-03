using Floudy.API.Storage.Entities;
using Floudy.API.Storage;
using MongoDB.Bson;

namespace Floudy.API.Services
{
    public class ChatService(ChatRepository repository)
    {
        public IEnumerable<ChatMessageEntity> GetRecent(int count = 100) => repository.GetAll().OrderByDescending(m => m.Timestamp).Take(count).Reverse().ToList();

        public ChatMessageEntity PostMessage(string username, string text)
        {
            var message = new ChatMessageEntity
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Username = username,
                Text = text,
                Timestamp = DateTime.UtcNow
            };
            repository.Add(message);
            return message;
        }

        public bool DeleteMessage(string id) => repository.Remove(id);
    }
}
