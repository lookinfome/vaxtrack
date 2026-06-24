using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vaxtrack.Models
{
    public class RevokedTokenModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Jti { get; set; } = "";        // JWT unique identifier claim
        public DateTime ExpiresAt { get; set; }      // when the token naturally expires (used for cleanup)
        public DateTime RevokedAt { get; set; }      // when logout was called
    }
}
