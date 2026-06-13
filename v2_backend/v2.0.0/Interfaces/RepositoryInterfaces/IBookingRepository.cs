

using Vaxtrack.Models;

namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface IBookingRepository
    {
        Task<BookingModel> CreateBookingAsync(BookingModel bookingCreateRequest);
        Task<BookingModel> UpdateBookingAsync(BookingModel bookingUpdateRequest);
        Task<BookingModel?> GetBookingDetailsByBookingId(string bookingId);
        Task<BookingModel?> GetBookingDetailsByUserUid(string userId);
        Task DeleteBookingAsync(BookingModel bookingDeleteRequest);
        Task<bool> IsBookingExists(string userId);
    }
}