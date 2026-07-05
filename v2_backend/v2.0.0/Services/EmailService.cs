using System.Net;
using System.Net.Mail;
using Vaxtrack.Interfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(SmtpSettings smtpSettings, ILogger<EmailService> logger)
        {
            _smtpSettings = smtpSettings;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            ArgumentNullException.ThrowIfNull(toEmail);
            ArgumentNullException.ThrowIfNull(resetLink);

            try
            {
                using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
                {
                    EnableSsl = _smtpSettings.EnableSsl,
                    Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.AppPassword)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName),
                    Subject = "VaxTrack — Reset your password",
                    Body = $"Click the link below to reset your password (expires in 15 minutes):\n\n{resetLink}\n\nIf you did not request this, you can safely ignore this email.",
                    IsBodyHtml = false
                };
                message.To.Add(toEmail);

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailService: SendPasswordResetEmailAsync - {Message}", ex.Message);
                throw;
            }
        }
    }
}
