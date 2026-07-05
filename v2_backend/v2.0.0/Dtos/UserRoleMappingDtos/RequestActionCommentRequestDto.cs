namespace Vaxtrack.Dtos.UserRoleMappingDtos
{
    // Shared optional-comment body for approving/rejecting a pending UserRequest
    // (reactivation or hospital-admin application). requestId stays a route parameter.
    public class RequestActionCommentRequestDto
    {
        public string? Comment { get; set; }
    }
}
