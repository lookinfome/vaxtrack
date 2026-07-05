namespace Vaxtrack.Dtos.UserDtos
{
    // Shared optional-comment body for user lifecycle actions (Disable / ApproveReactivation /
    // RejectReactivation). userId stays a route parameter — this only carries the admin's remark.
    public class UserActionCommentRequestDto
    {
        public string? Comment { get; set; }
    }
}
