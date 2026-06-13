

using Vaxtrack.Dtos.BookingDtos;

namespace Vaxtrack.Interfaces
{
    public interface IBookingService
    {
        // create booking
        Task<CreateBookingResponseDto> CreateBookingAsync(CreateBookingRequestDto createBookingReqeust);

        // update booking
        Task<UpdateBookingResponseDto> UpdateBookingAsync(UpdateBookingRequestDto updateBookingRequest);

        // get booking by booking id
        Task<BookingProfileDataDto?> GetBookingByBookingIdAsync(string bookingId);

        // get booking by user id
        Task<BookingProfileDataDto?> GetBookingsByUserIdAsync(string userId);

        // get bookings by hospital id
        Task<List<BookingProfileDataDto>?> GetBookingsByHospitalIdAsync(string hospitalId);

        // approve bookings
        Task<BookingProfileDataDto> ApproveBookingsAsync(string bookingId);

        // cancel bookings
        Task<BookingProfileDataDto> CancelBookingsAsync(string bookingId);

        // delete bookings
        Task DeleteBookingAsync(string bookingId);

        // is booking exists
        Task<bool> IsBookingExists(string bookingId);
    }
}