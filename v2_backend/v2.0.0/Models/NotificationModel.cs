using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vaxtrack.Models
{
    public class NotificationModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string RecipientUserUid { get; set; } = "";
        public string Message { get; set; } = "";
        public string? LinkPath { get; set; }   // optional frontend deep-link, e.g. "/hospital"
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; }
    }
}
