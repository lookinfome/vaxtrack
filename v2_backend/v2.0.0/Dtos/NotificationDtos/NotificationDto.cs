namespace Vaxtrack.Dtos.NotificationDtos
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Message { get; set; } = "";
        public string? LinkPath { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
