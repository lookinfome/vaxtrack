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

        // Rebooks a cancelled Dose 1 — the old slot was already freed when cancelled, so only the new slot is charged
        Task<BookingProfileDataDto> RebookDose1Async(RebookDose1RequestDto request, string callerUserUid);

        // callerUserUid enforces ownership; handles slot transfers when hospital changes
        Task<BookingProfileDataDto> EditBookingAsync(EditBookingRequestDto editBookingRequest, string callerUserUid);

        // callerUserUid + callerIsAdmin: platform admin OR hospital-admin scoped to the booking's hospital can approve
        Task<BookingProfileDataDto> ApproveBookingsAsync(string bookingId, string callerUserUid, bool callerIsAdmin, string? comment);

        // callerUserUid + callerIsAdmin: owner or admin can cancel
        Task<BookingProfileDataDto> CancelBookingsAsync(string bookingId, string callerUserUid, bool callerIsAdmin, string? comment);

        // Admin/hospital-admin only — declines a pending dose (distinct from a self-cancel), scoped like ApproveBookingsAsync
        Task<BookingProfileDataDto> RejectBookingAsync(string bookingId, string callerUserUid, bool callerIsAdmin, string? comment);

        // Chronological history of all lifecycle actions taken on a booking
        Task<List<BookingAuditLogDto>> GetBookingAuditTrailAsync(string bookingId, string callerUserUid, bool callerIsAdmin);

        // Bookings still needing action — scoped to caller's hospital-admin assignments, or all bookings for platform admin
        Task<List<BookingProfileDataDto>> GetActionableBookingsAsync(string callerUserUid, bool callerIsAdmin);

        // Read-only preview of the slot that would be assigned for this hospital+date — does not persist anything
        Task<NextAvailableSlotResponseDto> GetNextAvailableSlotAsync(string hospitalId, DateTime date);

        Task DeleteBookingAsync(string bookingId);
        Task DeleteBookingsByUserUidAsync(string userUid);
        Task<bool> IsBookingExists(string bookingId);
    }
}
