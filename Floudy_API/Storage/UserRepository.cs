using Floudy.API.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace Floudy.API.Storage
{
    public class UserRepository(string? connection_string = null)
    {
        public UserEntity? GetById(long id)
        {
            using var context = new AppDbContext(connection_string);
            return context.Users.Include(u => u.Role).FirstOrDefault(u => u.ID == id);
        }

        public IEnumerable<UserEntity> GetAll()
        {
            using var context = new AppDbContext(connection_string);
            return context.Users.Include(u => u.Role).ToList();
        }

        public void Add(UserEntity user)
        {
            using var context = new AppDbContext(connection_string);
            context.Users.Add(user);
            context.SaveChanges();
        }

        public void Update(UserEntity user)
        {
            using var context = new AppDbContext(connection_string);
            var existing = context.Users.Find(user.ID);
            if (existing != null)
            {
                existing.Username = user.Username;
                existing.Email = user.Email;
                existing.Password = user.Password;
                existing.IsBlocked = user.IsBlocked;
                context.SaveChanges();
            }
        }

        public UserEntity? GetByUsernameOrEmail(string identifier)
        {
            using var context = new AppDbContext(connection_string);
            return context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Username == identifier || u.Email == identifier);
        }

        public bool Any(Func<UserEntity, bool> predicate)
        {
            using var context = new AppDbContext(connection_string);
            return context.Users.Any(predicate);
        }
    }
}
