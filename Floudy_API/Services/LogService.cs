using Floudy.API.Storage;
using Floudy.API.Storage.Entities;

namespace Floudy.API.Services
{
    public class LogService(LogRepository repository, UserService user_service)
    {
        public void LogAction(string userId, string username, string groupName, string action, string description)
        {
            var entry = new LogEntryEntity
            {
                UserId = userId,
                Username = username,
                GroupName = groupName,
                Action = action,
                ActionDescription = description,
                Timestamp = DateTime.UtcNow
            };

            repository.AddLog(entry);
            PrintLog(entry);
        }

        public void PrintLog(LogEntryEntity entry)
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}");
            Console.ResetColor();
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"#{entry.UserId} ");
            Console.ResetColor();

            Console.Write("(");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(entry.Username);
            Console.ResetColor();
            Console.Write(") - ");

            Console.ForegroundColor = entry.GroupName == "Admin" ? ConsoleColor.Red : ConsoleColor.Blue;
            Console.Write(entry.GroupName);
            Console.ResetColor();
            Console.Write(": ");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{entry.Action}; {entry.ActionDescription}");
            Console.ResetColor();
        }

        public void FlagSuspiciousUser(string userId, string username, string reason)
        {
            var entity = new SuspiciousUserEntity
            {
                UserId = userId,
                Username = username,
                Reason = reason,
                DetectedAt = DateTime.UtcNow
            };

            repository.AddSuspicious(entity);

            Console.ResetColor();
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("SUSPICIOUS");
            Console.ResetColor();
            Console.Write("] ");

            LogAction(userId, username, user_service.GetByUsername(username)?.Role.Name ?? "unknown", reason, "");
        }

        public IEnumerable<LogEntryEntity> GetRecentLogs(int count = 100) => repository.GetAllLogs().OrderByDescending(e => e.Timestamp).Take(count).ToList();

        public IEnumerable<SuspiciousUserEntity> GetSuspiciousUsers() => repository.GetAllSuspicious().OrderByDescending(s => s.DetectedAt).ToList();
    }
}