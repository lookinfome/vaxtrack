namespace Vaxtrack.Models
{
    public class SmtpSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "VaxTrack";
        public string Username { get; set; } = "";
        public string AppPassword { get; set; } = "";
        public string FrontendResetPasswordUrl { get; set; } = "";
    }
}
