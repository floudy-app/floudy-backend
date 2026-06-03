using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Floudy.API.Storage.Entities
{
    public class SuspiciousUserEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        public string UserId { get; set; } = null!;

        public string Username { get; set; } = null!;

        public string Reason { get; set; } = null!;

        public DateTime DetectedAt { get; set; }
    }
}
