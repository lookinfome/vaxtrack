namespace Vaxtrack.Dtos.HospitalDtos
{
    // Shared optional-comment body for hospital lifecycle actions (Disable / RequestReactivation /
    // ApproveReactivation / RejectReactivation / RequestUnregister / WithdrawUnregister / DeclineUnregister).
    // hospitalId stays a route parameter — this only carries the actor's remark.
    public class HospitalActionCommentRequestDto
    {
        public string? Comment { get; set; }
    }
}
