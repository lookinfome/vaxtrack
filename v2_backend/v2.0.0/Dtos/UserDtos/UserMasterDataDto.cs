namespace Vaxtrack.Dtos.UserDtos
{
    public class UserMasterDataDto
    {
        public string UserId { get; set; } = "";

        public string UserName { get; set; } = "";

        public DateTime UserBirthdate { get; set; }

        public int UserAge { get; set; }

        public string UserUid { get; set; } = "";

        public string UserGender { get; set; } = "";

        public string UserPhone { get; set; } = "";

        public bool UserRole { get; set; } = true;

        public string ProfilePicturePath { get; set; } = "";
    }
}
