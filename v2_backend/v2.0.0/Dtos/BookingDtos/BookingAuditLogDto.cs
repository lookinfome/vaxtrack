namespace Vaxtrack.Dtos.BookingDtos
{
    public class BookingAuditLogDto
    {
        public string BookingId { get; set; } = "";
        public int DoseNumber { get; set; }
        public string ActionType { get; set; } = "";
        public string ActorUserUid { get; set; } = "";
        public string ActorRole { get; set; } = "";
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
