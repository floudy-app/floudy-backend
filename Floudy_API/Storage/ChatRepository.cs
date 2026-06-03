using Floudy.API.Storage.Entities;

namespace Floudy.API.Storage
{
    public class ChatRepository(string connection_string, string? database_name = null)
    {
        public IEnumerable<ChatMessageEntity> GetAll()
        {
            using var context = new ChatDbContext(connection_string, database_name);
            return context.Messages.ToList();
        }

        public void Add(ChatMessageEntity message)
        {
            using var context = new ChatDbContext(connection_string, database_name);
            context.Messages.Add(message);
            context.SaveChanges();
        }

        public bool Remove(string id)
        {
            using var context = new ChatDbContext(connection_string, database_name);
            var message = context.Messages.Find(id);
            if (message == null) return false;
            context.Messages.Remove(message);
            context.SaveChanges();
            return true;
        }

        public ChatMessageEntity? GetById(string id)
        {
            using var context = new ChatDbContext(connection_string, database_name);
            return context.Messages.Find(id);
        }
    }
}
