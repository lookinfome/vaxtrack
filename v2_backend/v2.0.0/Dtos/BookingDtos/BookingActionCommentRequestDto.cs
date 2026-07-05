namespace Vaxtrack.Dtos.BookingDtos
{
    // Shared optional-comment body for Approve / Reject / Cancel actions.
    // bookingId stays a route parameter — this only carries the admin/hospital-admin's remark.
    public class BookingActionCommentRequestDto
    {
        public string? Comment { get; set; }
    }
}
