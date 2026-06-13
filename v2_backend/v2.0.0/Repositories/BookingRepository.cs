

using Microsoft.EntityFrameworkCore;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Repositories
{
    public class BookingRepository: IBookingRepository
    {
        private readonly VaxtrackDbContext _dbContext;

        public BookingRepository(VaxtrackDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<BookingModel> CreateBookingAsync(BookingModel bookingCreateRequest)
        {
            ArgumentNullException.ThrowIfNull(bookingCreateRequest);

            _dbContext.Bookings.Add(bookingCreateRequest);
            _dbContext.SaveChanges();
            return bookingCreateRequest;
        }

        public async Task<BookingModel> UpdateBookingAsync(BookingModel bookingUpdateRequest)
        {
            ArgumentNullException.ThrowIfNull(bookingUpdateRequest);

            _dbContext.Bookings.Update(bookingUpdateRequest);
            await _dbContext.SaveChangesAsync();
            return bookingUpdateRequest;
        }

        public async Task<BookingModel?> GetBookingDetailsByBookingId(string bookingId)
        {
            ArgumentNullException.ThrowIfNull(bookingId);

            var foundBooking = await _dbContext.Bookings.Where(b=>b.BookingId == bookingId && !b.IsDeleted).FirstOrDefaultAsync();

            return foundBooking;
        }

        public async Task<BookingModel?> GetBookingDetailsByUserUid(string userId)
        {
            ArgumentNullException.ThrowIfNull(userId);

            var foundBooking = await _dbContext.Bookings.Where(b=>b.UserUid == userId && !b.IsDeleted).FirstOrDefaultAsync();
            return foundBooking;
        }

        public async Task DeleteBookingAsync(BookingModel bookingDeleteRequest)
        {
            ArgumentNullException.ThrowIfNull(bookingDeleteRequest);

            _dbContext.Bookings.Update(bookingDeleteRequest);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> IsBookingExists(string userUid)
        {
            ArgumentNullException.ThrowIfNull(userUid);

            return await _dbContext.Bookings.AnyAsync(b=>b.UserUid == userUid && !b.IsDeleted);
        }
    }
}