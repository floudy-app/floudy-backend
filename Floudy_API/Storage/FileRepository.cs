using Floudy.API.Storage.Entities;

namespace Floudy.API.Storage
{
    public class FileRepository(string connection_string)
    {
        public RawFileEntity? GetById(long id)
        {
            using var context = new AppDbContext(connection_string);
            return context.Files.Find(id);
        }

        public int CountByUserId(long userId)
        {
            using var context = new AppDbContext(connection_string);
            return context.Files.Count(f => f.UserId == userId);
        }

        public void Add(RawFileEntity entity)
        {
            using var context = new AppDbContext(connection_string);
            context.Files.Add(entity);
            context.SaveChanges();
        }

        public bool Remove(long id)
        {
            using var context = new AppDbContext(connection_string);
            var entity = context.Files.Find(id);
            if (entity == null) return false;
            context.Files.Remove(entity);
            context.SaveChanges();
            return true;
        }

        public void Update(RawFileEntity entity)
        {
            using var context = new AppDbContext(connection_string);
            var existing = context.Files.Find(entity.ID);
            if (existing != null)
            {
                existing.Name = entity.Name;
                context.SaveChanges();
            }
        }

        public bool BelongsToUser(long id, long userId)
        {
            using var context = new AppDbContext(connection_string);
            return context.Files.Any(x => x.ID == id && x.UserId == userId);
        }

        public IEnumerable<RawFileEntity> GetByUserId(long userId)
        {
            using var context = new AppDbContext(connection_string);
            return context.Files.Where(f => f.UserId == userId).OrderBy(x => x.ID).ToList();
        }
    }
}
