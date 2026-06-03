using Floudy.API.Storage.Entities;

namespace Floudy.API.Storage
{
    public class LogRepository(string connection_string)
    {
        public void AddLog(LogEntryEntity entry)
        {
            using var context = new LogDbContext(connection_string);
            context.LogEntries.Add(entry);
            context.SaveChanges();
        }

        public IEnumerable<LogEntryEntity> GetAllLogs()
        {
            using var context = new LogDbContext(connection_string);
            return context.LogEntries.ToList();
        }

        public void AddSuspicious(SuspiciousUserEntity entity)
        {
            using var context = new LogDbContext(connection_string);
            context.SuspiciousUsers.Add(entity);
            context.SaveChanges();
        }

        public IEnumerable<SuspiciousUserEntity> GetAllSuspicious()
        {
            using var context = new LogDbContext(connection_string);
            return context.SuspiciousUsers.ToList();
        }
    }
}
