using Vaxtrack.Dtos.NotificationDtos;

namespace Vaxtrack.Interfaces
{
    public interface INotificationService
    {
        // Fire-and-forget style creation, called from other services right alongside their
        // existing audit-log calls — never throws outward (a failed notification should not
        // fail the action that triggered it).
        Task NotifyAsync(string recipientUserUid, string message, string? linkPath = null);
        Task NotifyAllAdminsAsync(string message, string? linkPath = null);

        Task<List<NotificationDto>> GetMyNotificationsAsync(string callerUserUid);
        Task<int> GetUnreadCountAsync(string callerUserUid);
        Task MarkAsReadAsync(int id, string callerUserUid);
        Task MarkAllAsReadAsync(string callerUserUid);
    }
}
