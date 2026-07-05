using Microsoft.EntityFrameworkCore;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly VaxtrackDbContext _dbContext;
        private readonly ILogger<NotificationRepository> _logger;

        public NotificationRepository(VaxtrackDbContext dbContext, ILogger<NotificationRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<NotificationModel> CreateAsync(NotificationModel notification)
        {
            ArgumentNullException.ThrowIfNull(notification);

            try
            {
                _dbContext.Notifications.Add(notification);
                await _dbContext.SaveChangesAsync();
                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationRepository: CreateAsync - {Message}", ex.Message);
                throw new Exception($"NotificationRepository: CreateAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<NotificationModel>> GetByRecipientAsync(string recipientUserUid)
        {
            ArgumentNullException.ThrowIfNull(recipientUserUid);

            try
            {
                return await _dbContext.Notifications
                    .Where(n => n.RecipientUserUid == recipientUserUid)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(50)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationRepository: GetByRecipientAsync - {Message}", ex.Message);
                throw new Exception($"NotificationRepository: GetByRecipientAsync - {ex.Message}", ex);
            }
        }

        public async Task<int> GetUnreadCountAsync(string recipientUserUid)
        {
            ArgumentNullException.ThrowIfNull(recipientUserUid);

            try
            {
                return await _dbContext.Notifications
                    .Where(n => n.RecipientUserUid == recipientUserUid && !n.IsRead)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationRepository: GetUnreadCountAsync - {Message}", ex.Message);
                throw new Exception($"NotificationRepository: GetUnreadCountAsync - {ex.Message}", ex);
            }
        }

        public async Task<NotificationModel?> GetByIdAsync(int id)
        {
            try
            {
                return await _dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationRepository: GetByIdAsync - {Message}", ex.Message);
                throw new Exception($"NotificationRepository: GetByIdAsync - {ex.Message}", ex);
            }
        }

        public async Task MarkAsReadAsync(int id)
        {
            try
            {
                var notification = await _dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id);
                if (notification is null) return;

                notification.IsRead = true;
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationRepository: MarkAsReadAsync - {Message}", ex.Message);
                throw new Exception($"NotificationRepository: MarkAsReadAsync - {ex.Message}", ex);
            }
        }

        public async Task MarkAllAsReadAsync(string recipientUserUid)
        {
            ArgumentNullException.ThrowIfNull(recipientUserUid);

            try
            {
                var unread = await _dbContext.Notifications
                    .Where(n => n.RecipientUserUid == recipientUserUid && !n.IsRead)
                    .ToListAsync();

                foreach (var n in unread) n.IsRead = true;
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationRepository: MarkAllAsReadAsync - {Message}", ex.Message);
                throw new Exception($"NotificationRepository: MarkAllAsReadAsync - {ex.Message}", ex);
            }
        }
    }
}
