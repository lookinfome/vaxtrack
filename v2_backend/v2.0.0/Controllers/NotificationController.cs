using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vaxtrack.Dtos.NotificationDtos;
using Vaxtrack.Interfaces;

namespace Vaxtrack.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/vaxtrack/v1/[controller]/[action]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        private string CallerUserUid =>
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        [HttpGet]
        public async Task<ActionResult<List<NotificationDto>>> GetMyNotificationsAsync()
        {
            try
            {
                var notifications = await _notificationService.GetMyNotificationsAsync(CallerUserUid);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationController: GetMyNotificationsAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet]
        public async Task<ActionResult<int>> GetUnreadCountAsync()
        {
            try
            {
                var count = await _notificationService.GetUnreadCountAsync(CallerUserUid);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationController: GetUnreadCountAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> MarkAsReadAsync(int id)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(id, CallerUserUid);
                return NoContent();
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationController: MarkAsReadAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut]
        public async Task<ActionResult> MarkAllAsReadAsync()
        {
            try
            {
                await _notificationService.MarkAllAsReadAsync(CallerUserUid);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationController: MarkAllAsReadAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
