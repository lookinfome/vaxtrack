using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Models
{
    public class UserModel
    {
        [Key]
        public string UserId { get; set; } = "";

        public string UserName { get; set; } = "";

        public DateTime UserBirthdate { get; set; }

        public int UserAge {get; set;}

        public string UserUid { get; set; } = "";

        public string UserGender { get; set; } = "";

        public string UserPhone { get; set; } = "";

        public string UserAddress { get; set; } = "";

        public string UserPinCode { get; set; } = "";

        public bool UserRole { get; set; } = true;

        public string ProfilePicturePath { get; set; } = "";

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
