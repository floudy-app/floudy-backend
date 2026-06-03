using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Floudy.API.Storage.Entities
{
    public class LogEntryEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        public string UserId { get; set; } = null!;

        public string Username { get; set; } = null!;

        public string GroupName { get; set; } = null!;

        public string Action { get; set; } = null!;

        public string ActionDescription { get; set; } = null!;

        public DateTime Timestamp { get; set; }
    }
}
