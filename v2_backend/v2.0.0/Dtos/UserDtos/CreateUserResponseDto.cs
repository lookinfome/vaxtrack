namespace Vaxtrack.Dtos.UserDtos
{
    public class CreateUserResponseDto
    {
        public string UserId { get; set; } = "";

        public string UserName { get; set; } = "";

        public bool UserRole { get; set; } = false;
        public DateTime CreatedAt {get; set;}

    }
}
