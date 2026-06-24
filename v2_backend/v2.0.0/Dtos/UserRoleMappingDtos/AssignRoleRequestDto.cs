using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.UserRoleMappingDtos
{
    public class AssignRoleRequestDto
    {
        [Required(ErrorMessage = "user uid is required")]
        public string UserUid { get; set; } = "";

        [Required(ErrorMessage = "role tag is required")]
        public string RoleTag { get; set; } = "";

        // Optional — empty means global role; supply e.g. a HospitalId for scoped roles
        public string ContextId { get; set; } = "";
    }
}
