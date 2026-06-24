namespace Vaxtrack.Dtos.UserRoleMappingDtos
{
    public class AssignRoleResponseDto
    {
        public int Id { get; set; }
        public string UserUid { get; set; } = "";
        public string RoleTag { get; set; } = "";
        public string ContextId { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
