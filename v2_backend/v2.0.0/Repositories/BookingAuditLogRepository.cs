using Microsoft.EntityFrameworkCore;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Repositories
{
    public class BookingAuditLogRepository : IBookingAuditLogRepository
    {
        private readonly VaxtrackDbContext _dbContext;
        private readonly ILogger<BookingAuditLogRepository> _logger;

        public BookingAuditLogRepository(VaxtrackDbContext dbContext, ILogger<BookingAuditLogRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<BookingAuditLogModel> CreateEntryAsync(BookingAuditLogModel entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            try
            {
                _dbContext.BookingAuditLogs.Add(entry);
                await _dbContext.SaveChangesAsync();
                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingAuditLogRepository: CreateEntryAsync - {Message}", ex.Message);
                throw new Exception($"BookingAuditLogRepository: CreateEntryAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<BookingAuditLogModel>> GetEntriesByBookingIdAsync(string bookingId)
        {
            ArgumentNullException.ThrowIfNull(bookingId);

            try
            {
                return await _dbContext.BookingAuditLogs
                    .Where(e => e.BookingId == bookingId)
                    .OrderBy(e => e.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingAuditLogRepository: GetEntriesByBookingIdAsync - {Message}", ex.Message);
                throw new Exception($"BookingAuditLogRepository: GetEntriesByBookingIdAsync - {ex.Message}", ex);
            }
        }
    }
}
