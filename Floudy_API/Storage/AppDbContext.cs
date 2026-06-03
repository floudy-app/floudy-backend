using Floudy.API.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace Floudy.API.Storage
{
    public class AppDbContext : DbContext
    {
        private readonly string connection_string;

        public AppDbContext(string connection_string)
        {
            if (string.IsNullOrWhiteSpace(connection_string))
            {
                throw new ArgumentException("Connection string is required.", nameof(connection_string));
            }

            this.connection_string = connection_string;
            Database.EnsureCreated();
        }

        public DbSet<RawFileEntity> Files { get; set; } = null!;
        public DbSet<UserEntity> Users { get; set; } = null!;
        public DbSet<RoleEntity> Roles { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured) optionsBuilder.UseSqlServer(connection_string);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RawFileEntity>().HasKey(x => x.ID);
            modelBuilder.Entity<RawFileEntity>().Property(x => x.ID).ValueGeneratedNever();
            modelBuilder.Entity<RawFileEntity>()
                        .HasOne(x => x.User)
                        .WithMany()
                        .HasForeignKey(x => x.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserEntity>().HasKey(x => x.ID);
            modelBuilder.Entity<UserEntity>().Property(x => x.ID).ValueGeneratedNever();
            modelBuilder.Entity<UserEntity>().HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId);

            modelBuilder.Entity<RoleEntity>().HasKey(x => x.ID);
            modelBuilder.Entity<RoleEntity>().Property(x => x.ID).ValueGeneratedNever();

            modelBuilder.Entity<RoleEntity>().HasData(
                new RoleEntity { ID = 1, Name = "Admin" },
                new RoleEntity { ID = 2, Name = "User" }
            );

            modelBuilder.Entity<UserEntity>().HasData(
                new UserEntity
                {
                    ID = 1,
                    Username = "admin",
                    Email = "prooms.email@gmail.com",
                    Password = "admin",
                    RoleId = 1,
                    IsBlocked = false
                }
            );
        }
    }
}
