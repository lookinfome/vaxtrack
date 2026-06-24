using Microsoft.EntityFrameworkCore;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly VaxtrackDbContext _dbContext;
        private readonly ILogger<BookingRepository> _logger;

        public BookingRepository(VaxtrackDbContext dbContext, ILogger<BookingRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<BookingModel> CreateBookingAsync(BookingModel bookingCreateRequest)
        {
            ArgumentNullException.ThrowIfNull(bookingCreateRequest);

            try
            {
                _dbContext.Bookings.Add(bookingCreateRequest);
                await _dbContext.SaveChangesAsync();
                return bookingCreateRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingRepository: CreateBookingAsync - {Message}", ex.Message);
                throw new Exception($"BookingRepository: CreateBookingAsync - {ex.Message}", ex);
            }
        }

        public async Task<BookingModel> UpdateBookingAsync(BookingModel bookingUpdateRequest)
        {
            ArgumentNullException.ThrowIfNull(bookingUpdateRequest);

            try
            {
                _dbContext.Bookings.Update(bookingUpdateRequest);
                await _dbContext.SaveChangesAsync();
                return bookingUpdateRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingRepository: UpdateBookingAsync - {Message}", ex.Message);
                throw new Exception($"BookingRepository: UpdateBookingAsync - {ex.Message}", ex);
            }
        }

        public async Task<BookingModel?> GetBookingDetailsByBookingIdAsync(string bookingId)
        {
            ArgumentNullException.ThrowIfNull(bookingId);

            try
            {
                return await _dbContext.Bookings
                    .Where(b => b.BookingId == bookingId && !b.IsDeleted)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingRepository: GetBookingDetailsByBookingIdAsync - {Message}", ex.Message);
                throw new Exception($"BookingRepository: GetBookingDetailsByBookingIdAsync - {ex.Message}", ex);
            }
        }

        public async Task<BookingModel?> GetBookingDetailsByUserUidAsync(string userUid)
        {
            ArgumentNullException.ThrowIfNull(userUid);

            try
            {
                return await _dbContext.Bookings
                    .Where(b => b.UserUid == userUid && !b.IsDeleted)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingRepository: GetBookingDetailsByUserUidAsync - {Message}", ex.Message);
                throw new Exception($"BookingRepository: GetBookingDetailsByUserUidAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<BookingModel>?> GetBookingDetailsByHospitalUidAsync(string hospitalId)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                // Parentheses required — && binds tighter than ||, so without them
                // the IsDeleted filter would only apply to Dose2HospitalUid matches
                return await _dbContext.Bookings
                    .Where(b => (b.Dose1HospitalUid == hospitalId || b.Dose2HospitalUid == hospitalId) && !b.IsDeleted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingRepository: GetBookingDetailsByHospitalUidAsync - {Message}", ex.Message);
                throw new Exception($"BookingRepository: GetBookingDetailsByHospitalUidAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<BookingModel>?> GetAllBookingDetailsAsync()
        {
            try
            {
                return await _dbContext.Bookings.Where(b => !b.IsDeleted).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingRepository: GetAllBookingDetailsAsync - {Message}", ex.Message);
                throw new Exception($"BookingRepository: GetAllBookingDetailsAsync - {ex.Message}", ex);
            }
        }

        public async Task DeleteBookingAsync(BookingModel bookingDeleteRequest)
        {
            ArgumentNullException.ThrowIfNull(bookingDeleteRequest);

            try
            {
                _dbContext.Bookings.Update(bookingDeleteRequest);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingRepository: DeleteBookingAsync - {Message}", ex.Message);
                throw new Exception($"BookingRepository: DeleteBookingAsync - {ex.Message}", ex);
            }
        }

        public async Task<bool> IsBookingExistsAsync(string userUid)
        {
            ArgumentNullException.ThrowIfNull(userUid);

            try
            {
                return await _dbContext.Bookings.AnyAsync(b => b.UserUid == userUid && !b.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingRepository: IsBookingExistsAsync - {Message}", ex.Message);
                throw new Exception($"BookingRepository: IsBookingExistsAsync - {ex.Message}", ex);
            }
        }
    }
}
