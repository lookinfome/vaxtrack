using Vaxtrack.Dtos.NotificationDtos;
using Vaxtrack.Interfaces;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(INotificationRepository notificationRepository, IUserRepository userRepository, ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task NotifyAsync(string recipientUserUid, string message, string? linkPath = null)
        {
            // Best-effort: a notification failure must never fail the caller's real action.
            try
            {
                if (string.IsNullOrWhiteSpace(recipientUserUid)) return;

                await _notificationRepository.CreateAsync(new NotificationModel
                {
                    RecipientUserUid = recipientUserUid,
                    Message = message,
                    LinkPath = linkPath,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService: NotifyAsync - {Message}", ex.Message);
            }
        }

        public async Task NotifyAllAdminsAsync(string message, string? linkPath = null)
        {
            try
            {
                var allUsers = await _userRepository.GetAllUsersDetailAsync() ?? [];
                foreach (var admin in allUsers.Where(u => u.UserRole))
                    await NotifyAsync(admin.UserUid, message, linkPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService: NotifyAllAdminsAsync - {Message}", ex.Message);
            }
        }

        public async Task<List<NotificationDto>> GetMyNotificationsAsync(string callerUserUid)
        {
            ArgumentNullException.ThrowIfNull(callerUserUid);

            try
            {
                var entries = await _notificationRepository.GetByRecipientAsync(callerUserUid);
                return entries.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService: GetMyNotificationsAsync - {Message}", ex.Message);
                throw new Exception($"NotificationService: GetMyNotificationsAsync - {ex.Message}", ex);
            }
        }

        public async Task<int> GetUnreadCountAsync(string callerUserUid)
        {
            ArgumentNullException.ThrowIfNull(callerUserUid);

            try
            {
                return await _notificationRepository.GetUnreadCountAsync(callerUserUid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService: GetUnreadCountAsync - {Message}", ex.Message);
                throw new Exception($"NotificationService: GetUnreadCountAsync - {ex.Message}", ex);
            }
        }

        public async Task MarkAsReadAsync(int id, string callerUserUid)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(id);
                if (notification is null) return;

                if (notification.RecipientUserUid != callerUserUid)
                    throw new UnauthorizedAccessException("NotificationService: MarkAsReadAsync - caller does not own this notification");

                await _notificationRepository.MarkAsReadAsync(id);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService: MarkAsReadAsync - {Message}", ex.Message);
                throw new Exception($"NotificationService: MarkAsReadAsync - {ex.Message}", ex);
            }
        }

        public async Task MarkAllAsReadAsync(string callerUserUid)
        {
            ArgumentNullException.ThrowIfNull(callerUserUid);

            try
            {
                await _notificationRepository.MarkAllAsReadAsync(callerUserUid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService: MarkAllAsReadAsync - {Message}", ex.Message);
                throw new Exception($"NotificationService: MarkAllAsReadAsync - {ex.Message}", ex);
            }
        }

        private static NotificationDto MapToDto(NotificationModel n)
        {
            return new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                LinkPath = n.LinkPath,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            };
        }
    }
}
