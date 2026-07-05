using Vaxtrack.Models;

namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface INotificationRepository
    {
        Task<NotificationModel> CreateAsync(NotificationModel notification);
        Task<List<NotificationModel>> GetByRecipientAsync(string recipientUserUid);
        Task<int> GetUnreadCountAsync(string recipientUserUid);
        Task<NotificationModel?> GetByIdAsync(int id);
        Task MarkAsReadAsync(int id);
        Task MarkAllAsReadAsync(string recipientUserUid);
    }
}
