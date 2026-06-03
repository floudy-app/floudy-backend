using Floudy.API.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace Floudy.API.Storage
{
    public class LogDbContext : DbContext
    {
        private readonly string connection_string;

        public LogDbContext(string connection_string)
        {
            if (string.IsNullOrWhiteSpace(connection_string))
            {
                throw new ArgumentException("Connection string is required.", nameof(connection_string));
            }

            this.connection_string = connection_string;
            Database.EnsureCreated();
        }

        public DbSet<LogEntryEntity> LogEntries { get; set; } = null!;
        public DbSet<SuspiciousUserEntity> SuspiciousUsers { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured) optionsBuilder.UseSqlServer(connection_string);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LogEntryEntity>().HasKey(x => x.ID);
            modelBuilder.Entity<SuspiciousUserEntity>().HasKey(x => x.ID);
        }
    }
}
