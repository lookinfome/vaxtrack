namespace Vaxtrack.Dtos.UserRoleMappingDtos
{
    public class UserRequestDto
    {
        public int Id { get; set; }
        public string UserUid { get; set; } = "";
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string RequestType { get; set; } = "";
        public string? TargetHospitalId { get; set; }
        public string Status { get; set; } = "";
        public string? UserComment { get; set; }
        public string? AdminComment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolvedByUserUid { get; set; }
    }
}
