using Vaxtrack.Dtos.BookingDtos;

namespace Vaxtrack.Interfaces
{
    public interface IBookingService
    {
        // callerUserUid enforces that a user can only create bookings for themselves
        Task<CreateBookingResponseDto> CreateBookingAsync(CreateBookingRequestDto createBookingRequest, string callerUserUid);

        // Admin-only full field override
        Task<UpdateBookingResponseDto> UpdateBookingAsync(UpdateBookingRequestDto updateBookingRequest);

        Task<BookingProfileDataDto?> GetBookingByBookingIdAsync(string bookingId, string callerUserUid, bool callerIsAdmin);
        Task<BookingProfileDataDto?> GetBookingsByUserIdAsync(string userId, string callerUserUid, bool callerIsAdmin);
        Task<List<BookingProfileDataDto>?> GetBookingsByHospitalIdAsync(string hospitalId);
        Task<List<BookingProfileDataDto>?> GetAllBookingsAsync();

        // callerUserUid enforces that only the booking owner can schedule Dose 2
        Task<BookDose2ResponseDto> BookDose2Async(BookDose2RequestDto bookDose2Request, string callerUserUid);

        // callerUserUid + callerIsAdmin: platform admin OR hospital-admin scoped to the booking's hospital can approve
        Task<BookingProfileDataDto> ApproveBookingsAsync(string bookingId, string callerUserUid, bool callerIsAdmin);

        // callerUserUid + callerIsAdmin: owner or admin can cancel
        Task<BookingProfileDataDto> CancelBookingsAsync(string bookingId, string callerUserUid, bool callerIsAdmin);

        Task DeleteBookingAsync(string bookingId);
        Task DeleteBookingsByUserUidAsync(string userUid);
        Task<bool> IsBookingExists(string bookingId);
    }
}
