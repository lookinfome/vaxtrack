

using Vaxtrack.Models;

namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface IBookingRepository
    {
        Task<BookingModel> CreateBookingAsync(BookingModel bookingCreateRequest);
        Task<BookingModel> UpdateBookingAsync(BookingModel bookingUpdateRequest);
        Task<BookingModel?> GetBookingDetailsByBookingIdAsync(string bookingId);
        Task<BookingModel?> GetBookingDetailsByUserUidAsync(string userId);
        Task<List<BookingModel>?> GetBookingDetailsByHospitalUidAsync(string hospitalId);
        Task<List<BookingModel>?> GetAllBookingDetailsAsync();
        Task<List<BookingModel>> GetAllActiveBookingsByUserUidAsync(string userUid);
        Task<bool> HasActiveBookingsForHospitalAsync(string hospitalId);
        Task DeleteBookingAsync(BookingModel bookingDeleteRequest);
        Task<bool> IsBookingExistsAsync(string userId);
        Task<DateTime?> GetLatestSlotEndTimeForHospitalAndDateAsync(string hospitalId, DateTime date);
    }
}