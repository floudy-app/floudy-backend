using System.Text;
using System.Text.RegularExpressions;

namespace Floudy.API.Services
{
    public class MaliciousDetectionService
    {
        private readonly LogService log_service;
        private readonly HashSet<string> suspicious_extensions;
        private readonly List<string> swear_patterns;

        private const long ONE_GB = 1_073_741_824;
        private const int MAX_BATCH_SIZE = 10;

        public MaliciousDetectionService(LogService log_service)
        {
            this.log_service = log_service;

            var baseDir = AppContext.BaseDirectory;

            var extension_path = Path.Combine(baseDir, "extensions.txt");
            if (!File.Exists(extension_path)) extension_path = Path.Combine(Directory.GetParent(baseDir)!.Parent!.Parent!.Parent!.FullName, "extensions.txt");

            var profPath = Path.Combine(baseDir, "profanity.txt");
            if (!File.Exists(profPath)) profPath = Path.Combine(Directory.GetParent(baseDir)!.Parent!.Parent!.Parent!.FullName, "profanity.txt");

            suspicious_extensions = File.Exists(extension_path) ? new HashSet<string>(File.ReadAllLines(extension_path).Select(l => l.Trim()), StringComparer.OrdinalIgnoreCase)
                                                                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            swear_patterns = File.Exists(profPath) ? File.ReadAllLines(profPath)
                                                         .Select(l => Encoding.UTF8.GetString(Convert.FromBase64String(l.Trim())))
                                                         .ToList()
                                                   : [];
        }

        public void CheckBatchUpload(string userId, string username, int fileCount, IEnumerable<string> fileNames, IEnumerable<long> fileSizes)
        {
            if (fileCount > MAX_BATCH_SIZE)
            {
                log_service.FlagSuspiciousUser(userId, username, $"Uploaded {fileCount} files in a single batch (limit: {MAX_BATCH_SIZE})");
            }

            var names = fileNames.ToList();
            var sizes = fileSizes.ToList();

            for (int i = 0; i < names.Count; i++)
            {
                var ext = Path.GetExtension(names[i]);
                if (!string.IsNullOrEmpty(ext) && suspicious_extensions.Contains(ext)) 
                    log_service.FlagSuspiciousUser(userId, username, $"Uploaded file with suspicious extension: {names[i]}");
                
                if (i < sizes.Count && sizes[i] >= ONE_GB) 
                    log_service.FlagSuspiciousUser(userId, username, $"Uploaded very large file ({sizes[i] / (1024.0 * 1024.0 * 1024.0):F2} GB): {names[i]}");
            }
        }

        public void CheckChatMessage(string userId, string username, string text)
        {
            foreach (var word in swear_patterns)
            {
                if (Regex.IsMatch(text, $@"{Regex.Escape(word)}", RegexOptions.IgnoreCase))
                {
                    log_service.FlagSuspiciousUser(userId, username, $"Used profanity in the chat");
                    return;
                }
            }
        }
    }
}
