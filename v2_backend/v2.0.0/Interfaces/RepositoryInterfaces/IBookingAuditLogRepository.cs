using Vaxtrack.Models;

namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface IBookingAuditLogRepository
    {
        Task<BookingAuditLogModel> CreateEntryAsync(BookingAuditLogModel entry);
        Task<List<BookingAuditLogModel>> GetEntriesByBookingIdAsync(string bookingId);
    }
}
