namespace Vaxtrack.Dtos.AuthDtos
{
    public class ForgotPasswordResponseDto
    {
        // Always shown — same message regardless of whether the email is registered (prevents enumeration)
        public string Message { get; set; } = "";

        // Always null now — the reset token is emailed directly to the user via IEmailService
        // and never returned in the API response. Kept nullable for frontend model compatibility.
        public string? ResetToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
