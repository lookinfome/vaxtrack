namespace Vaxtrack.Dtos.HospitalDtos
{
    public class HospitalAuditLogDto
    {
        public string HospitalId { get; set; } = "";
        public string ActionType { get; set; } = "";
        public string ActorUserUid { get; set; } = "";
        public string ActorRole { get; set; } = "";
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
