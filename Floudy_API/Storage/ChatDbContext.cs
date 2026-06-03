using Floudy.API.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Floudy.API.Storage
{
    public class ChatDbContext : DbContext
    {
        private readonly string connection_string;
        private readonly string database_name;

        public ChatDbContext(string connection_string, string? database_name = null)
        {
            if (string.IsNullOrWhiteSpace(connection_string))
            {
                throw new ArgumentException("Connection string is required.", nameof(connection_string));
            }

            this.connection_string = connection_string;
            this.database_name = database_name ?? "floudy_chat";
        }

        public DbSet<ChatMessageEntity> Messages { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var client = new MongoClient(connection_string);
                optionsBuilder.UseMongoDB(client, database_name);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatMessageEntity>().ToCollection("messages").HasKey(m => m.Id);
        }
    }
}
