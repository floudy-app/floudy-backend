using Floudy.API.Models;
using Floudy.API.Storage;
using Floudy.API.Storage.Entities;
using Floudy.API.Utility;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Floudy.API.Services
{
    public class FileService(FileRepository repository)
    {
        public const int PAGE_FILE_COUNT = 10;

        public FileRepository Files { get; } = repository;

        private static RawFile MapToModel(RawFileEntity entity) => new
        (
            entity.ID,
            entity.Name,
            entity.Content?.Length ?? 0,
            entity.Type,
            entity.UploadDate,
            entity.Content!
        );

        public bool VerifyMetadata(Dictionary<string, object>? metadata)
        {
            return metadata != null &&

                   metadata.ContainsKey("raw") &&
                   metadata["raw"] is byte[] raw &&
                   raw != null &&

                   metadata["size"] is JsonElement size &&
                   metadata["uploaded"] is JsonElement upload_date_ms &&

                   size.GetInt64() >= 0 &&
                   upload_date_ms.GetInt64() > 0 &&

                   !string.IsNullOrEmpty(metadata["name"].ToString()) &&
                   !string.IsNullOrWhiteSpace(metadata["name"].ToString());
        }

        public bool VerifyMetadata(RawFileMetadata metadata) => !string.IsNullOrEmpty(metadata.Name);

        public bool UploadFromMetadata(Dictionary<string, object>? metadata, long userId)
        {
            if (!VerifyMetadata(metadata)) return false;

            var entity = new RawFileEntity
            {
                ID = GlobalIdManager.Register(),
                Name = metadata!["name"].ToString()!,
                Type = string.IsNullOrWhiteSpace(metadata["type"].ToString()) ? Regex.Replace(metadata!["name"].ToString()!, @".*\.[^\.]+$", "") 
                                                                              : metadata["type"].ToString()!,
                UploadDate = DateTimeOffset.FromUnixTimeMilliseconds(((JsonElement)metadata["uploaded"]).GetInt64()).LocalDateTime,
                Content = (byte[])metadata["raw"],
                UserId = userId
            };

            repository.Add(entity);
            return true;
        }

        public bool BelongsToUser(long fileId, long userId) => repository.BelongsToUser(fileId, userId);

        public bool ModifyMetadata(long file_ID, RawFileMetadata new_metadata, long userId)
        {
            if (!repository.BelongsToUser(file_ID, userId) || !VerifyMetadata(new_metadata)) return false;

            repository.Update(new RawFileEntity
            {
                ID = file_ID,
                Name = new_metadata.Name
            });
            return true;
        }

        public bool DeleteFile(long file_ID, long userId)
        {
            if (!repository.BelongsToUser(file_ID, userId)) return false;

            var valid = repository.Remove(file_ID);
            if (valid) GlobalIdManager.Unregister(file_ID);
            return valid;
        }

        public RawFile GetById(long file_ID, long userId)
        {
            var entity = repository.GetById(file_ID);
            if (entity == null || entity.UserId != userId) throw new KeyNotFoundException();
            return MapToModel(entity);
        }

        public IEnumerable<RawFile> GetPage(int page_index, long userId) => 
            page_index < 1 ? [] : repository.GetByUserId(userId).Skip((page_index - 1) * PAGE_FILE_COUNT).Take(PAGE_FILE_COUNT).Select(MapToModel);

        public int TotalPageCount(long userId) => Math.Clamp((repository.CountByUserId(userId) + PAGE_FILE_COUNT - 1) / PAGE_FILE_COUNT, 1, int.MaxValue);

        public IEnumerable<RawFile> GetRecent(int count, long userId) => repository.GetByUserId(userId).OrderByDescending(f => f.UploadDate).Take(count).Select(MapToModel);

        public Dictionary<long, int> GetFileIdPositionMap(long userId)
        {
            var allIds = repository.GetByUserId(userId).Select(f => f.ID).ToList();
            var map = new Dictionary<long, int>();
            for (int i = 0; i < allIds.Count; i++) map[allIds[i]] = i + 1;
            return map;
        }

        public Dictionary<string, int> TypeStatistics(long userId) => 
            repository.GetByUserId(userId).GroupBy(x => Regex.Replace(x.Type, @"\/.*$", "")).ToDictionary(x => x.Key, x => x.Count());

        public Dictionary<DateTime, int> UploadStatistics(long userId) => 
            repository.GetByUserId(userId).GroupBy(x => new DateTime(x.UploadDate.Year, x.UploadDate.Month, 1)).OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Count());
    }
}
