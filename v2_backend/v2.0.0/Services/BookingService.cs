using Vaxtrack.Interfaces;
using Vaxtrack.Dtos.BookingDtos;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;
using Vaxtrack.Interfaces.UtilityInterfaces;

namespace Vaxtrack.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IUtilityService _utilityService;
        private readonly IHospitalService _hospitalService;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            IBookingRepository bookingRepository,
            IUtilityService utilityService,
            IHospitalService hospitalService,
            ILogger<BookingService> logger)
        {
            _bookingRepository = bookingRepository;
            _utilityService = utilityService;
            _hospitalService = hospitalService;
            _logger = logger;
        }

        public async Task<CreateBookingResponseDto> CreateBookingAsync(CreateBookingRequestDto createBookingRequest)
        {
            ArgumentNullException.ThrowIfNull(createBookingRequest);

            try
            {
                // Enforce one active booking per user — duplicate bookings are not allowed
                bool userAlreadyBooked = await _bookingRepository.IsBookingExistsAsync(createBookingRequest.UserUid);
                if (userAlreadyBooked)
                    throw new Exception($"BookingService: CreateBookingAsync - user {createBookingRequest.UserUid} already has an active booking");

                // Validate the hospital has capacity before reserving a slot
                var hospital = await _hospitalService.GetHospitalByIdAsync(createBookingRequest.Dose1HospitalUid);
                if (hospital.SlotsAvailable <= 0)
                    throw new Exception($"BookingService: CreateBookingAsync - hospital {createBookingRequest.Dose1HospitalUid} has no available slots");

                var newBooking = await MapCreateBookingRequestToBookingModel(createBookingRequest);
                var createdBooking = await _bookingRepository.CreateBookingAsync(newBooking);

                // Decrement available slot count by 1 after confirming the booking
                await _hospitalService.UpdateAvailableSlotsAsync(createBookingRequest.Dose1HospitalUid, -1);

                return MapToCreateBookingResponseDto(createdBooking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: CreateBookingAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: CreateBookingAsync - {ex.Message}", ex);
            }
        }

        public async Task<BookDose2ResponseDto> BookDose2Async(BookDose2RequestDto bookDose2Request)
        {
            ArgumentNullException.ThrowIfNull(bookDose2Request);

            try
            {
                var foundBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookDose2Request.BookingId);

                if (foundBooking == null)
                    throw new Exception($"BookingService: BookDose2Async - booking {bookDose2Request.BookingId} not found");

                // Dose 1 must be physically administered before Dose 2 can be scheduled
                if (!foundBooking.IsDose1Completed)
                    throw new Exception($"BookingService: BookDose2Async - Dose 1 must be completed before booking Dose 2 for booking {bookDose2Request.BookingId}");

                // Prevent re-booking Dose 2 if it is already scheduled
                if (!string.IsNullOrEmpty(foundBooking.Dose2HospitalUid))
                    throw new Exception($"BookingService: BookDose2Async - Dose 2 is already booked for booking {bookDose2Request.BookingId}");

                if (foundBooking.IsDose2Completed)
                    throw new Exception($"BookingService: BookDose2Async - Dose 2 is already completed for booking {bookDose2Request.BookingId}");

                if (foundBooking.IsD1RequestCanceled)
                    throw new Exception($"BookingService: BookDose2Async - booking {bookDose2Request.BookingId} Dose 1 was canceled");

                // Validate the Dose 2 hospital has available slots
                var hospital = await _hospitalService.GetHospitalByIdAsync(bookDose2Request.Dose2HospitalUid);
                if (hospital.SlotsAvailable <= 0)
                    throw new Exception($"BookingService: BookDose2Async - hospital {bookDose2Request.Dose2HospitalUid} has no available slots");

                var mappedBooking = MapToDose2BookingModel(foundBooking, bookDose2Request);
                var updatedBooking = await _bookingRepository.UpdateBookingAsync(mappedBooking);

                // Decrement Dose 2 hospital slot count by 1
                await _hospitalService.UpdateAvailableSlotsAsync(bookDose2Request.Dose2HospitalUid, -1);

                return MapToBookDose2ResponseDto(updatedBooking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: BookDose2Async - {Message}", ex.Message);
                throw new Exception($"BookingService: BookDose2Async - {ex.Message}", ex);
            }
        }

        public async Task<BookingProfileDataDto> ApproveBookingsAsync(string bookingId)
        {
            /*
             * Approve Logic:
             * --------------
             * "Approve" means a hospital admin confirms that a dose was physically administered to the user.
             *
             * Priority order:
             *   1. If Dose 1 is pending (not completed, not canceled) → approve Dose 1.
             *      Sets IsDose1Completed = true and records Dose1CompletedDateTime.
             *
             *   2. If Dose 1 is already done AND Dose 2 is booked but pending
             *      (not completed, not canceled) → approve Dose 2.
             *      Sets IsDose2Completed = true, IsVaccinationCompleted = true, records timestamps.
             *
             * Edge cases blocked:
             *   - Booking not found                            → throws
             *   - Vaccination already fully completed          → throws (nothing left to approve)
             *   - Dose 1 canceled                             → falls to else branch  → throws
             *   - Dose 1 complete but Dose 2 not yet booked   → falls to else branch  → throws
             *   - Dose 2 already canceled or completed         → falls to else branch  → throws
             */

            ArgumentNullException.ThrowIfNull(bookingId);

            try
            {
                var foundBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookingId);

                if (foundBooking == null)
                    throw new Exception($"BookingService: ApproveBookingsAsync - booking {bookingId} not found");

                if (foundBooking.IsVaccinationCompleted)
                    throw new Exception($"BookingService: ApproveBookingsAsync - booking {bookingId} vaccination is already fully completed");

                var timestamp = DateTime.UtcNow;

                if (!foundBooking.IsDose1Completed && !foundBooking.IsD1RequestCanceled)
                {
                    // Approve Dose 1 — mark it as administered
                    foundBooking.IsDose1Completed = true;
                    foundBooking.Dose1CompletedDateTime = timestamp;
                }
                else if (foundBooking.IsDose1Completed
                    && !string.IsNullOrEmpty(foundBooking.Dose2HospitalUid)
                    && !foundBooking.IsDose2Completed
                    && !foundBooking.IsD2RequestCanceled)
                {
                    // Approve Dose 2 — mark it as administered and flag full vaccination complete
                    foundBooking.IsDose2Completed = true;
                    foundBooking.Dose2CompletedDateTime = timestamp;
                    foundBooking.IsVaccinationCompleted = true;
                    foundBooking.VaccinationCompletedDateTime = timestamp;
                }
                else
                {
                    throw new Exception($"BookingService: ApproveBookingsAsync - booking {bookingId} has no pending dose to approve");
                }

                foundBooking.ModifiedAt = timestamp;
                var updatedBooking = await _bookingRepository.UpdateBookingAsync(foundBooking);
                return MapToBookingProfileDto(updatedBooking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: ApproveBookingsAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: ApproveBookingsAsync - {ex.Message}", ex);
            }
        }

        public async Task<BookingProfileDataDto> CancelBookingsAsync(string bookingId)
        {
            /*
             * Cancel Logic:
             * -------------
             * "Cancel" means a user or admin withdraws a pending dose request before it is administered.
             * Canceling restores the hospital's available slot so another user can book it.
             *
             * Priority order:
             *   1. If Dose 1 is not yet completed and not already canceled → cancel Dose 1.
             *      Sets IsD1RequestCanceled = true and restores +1 slot to the Dose 1 hospital.
             *
             *   2. If Dose 1 is already completed AND Dose 2 is booked but not yet completed
             *      and not already canceled → cancel Dose 2.
             *      Sets IsD2RequestCanceled = true and restores +1 slot to the Dose 2 hospital.
             *
             * Edge cases blocked:
             *   - Booking not found                                         → throws
             *   - Vaccination already fully completed                       → throws (cannot undo an administered vaccine)
             *   - Dose 1 already canceled and no Dose 2 booked             → falls to else branch → throws
             *   - Dose 1 complete but Dose 2 not yet booked                → falls to else branch → throws
             *   - Dose 2 already canceled or completed                      → falls to else branch → throws
             */

            ArgumentNullException.ThrowIfNull(bookingId);

            try
            {
                var foundBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookingId);

                if (foundBooking == null)
                    throw new Exception($"BookingService: CancelBookingsAsync - booking {bookingId} not found");

                if (foundBooking.IsVaccinationCompleted)
                    throw new Exception($"BookingService: CancelBookingsAsync - booking {bookingId} vaccination is already completed and cannot be canceled");

                var timestamp = DateTime.UtcNow;

                if (!foundBooking.IsDose1Completed && !foundBooking.IsD1RequestCanceled)
                {
                    // Cancel Dose 1 and free up the reserved hospital slot
                    foundBooking.IsD1RequestCanceled = true;
                    foundBooking.ModifiedAt = timestamp;
                    await _bookingRepository.UpdateBookingAsync(foundBooking);
                    await _hospitalService.UpdateAvailableSlotsAsync(foundBooking.Dose1HospitalUid, 1);
                }
                else if (foundBooking.IsDose1Completed
                    && !string.IsNullOrEmpty(foundBooking.Dose2HospitalUid)
                    && !foundBooking.IsDose2Completed
                    && !foundBooking.IsD2RequestCanceled)
                {
                    // Cancel Dose 2 and free up the reserved hospital slot
                    foundBooking.IsD2RequestCanceled = true;
                    foundBooking.ModifiedAt = timestamp;
                    await _bookingRepository.UpdateBookingAsync(foundBooking);
                    await _hospitalService.UpdateAvailableSlotsAsync(foundBooking.Dose2HospitalUid, 1);
                }
                else
                {
                    throw new Exception($"BookingService: CancelBookingsAsync - booking {bookingId} has no active dose request to cancel");
                }

                var updatedBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookingId);
                return MapToBookingProfileDto(updatedBooking!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: CancelBookingsAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: CancelBookingsAsync - {ex.Message}", ex);
            }
        }

        public async Task<UpdateBookingResponseDto> UpdateBookingAsync(UpdateBookingRequestDto updateBookingRequest)
        {
            /*
             * Update Logic:
             * -------------
             * General-purpose admin update — all booking fields can be overridden
             * except identity fields (BookingId, UserUid, CreatedAt) which are immutable.
             *
             * Edge cases blocked:
             *   - Booking not found → throws
             */

            ArgumentNullException.ThrowIfNull(updateBookingRequest);

            try
            {
                var foundBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(updateBookingRequest.BookingId);

                if (foundBooking == null)
                    throw new Exception($"BookingService: UpdateBookingAsync - booking {updateBookingRequest.BookingId} not found");

                var mappedBooking = MapUpdateBookingRequestToBookingModel(foundBooking, updateBookingRequest);
                var updatedBooking = await _bookingRepository.UpdateBookingAsync(mappedBooking);
                return MapToUpdateBookingResponseDto(updatedBooking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: UpdateBookingAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: UpdateBookingAsync - {ex.Message}", ex);
            }
        }

        public async Task<BookingProfileDataDto?> GetBookingByBookingIdAsync(string bookingId)
        {
            ArgumentNullException.ThrowIfNull(bookingId);

            try
            {
                var foundBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookingId);
                if (foundBooking == null)
                    throw new Exception($"BookingService: GetBookingByBookingIdAsync - booking {bookingId} not found");
                return MapToBookingProfileDto(foundBooking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: GetBookingByBookingIdAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: GetBookingByBookingIdAsync - {ex.Message}", ex);
            }
        }

        public async Task<BookingProfileDataDto?> GetBookingsByUserIdAsync(string userId)
        {
            ArgumentNullException.ThrowIfNull(userId);

            try
            {
                var foundBooking = await _bookingRepository.GetBookingDetailsByUserUidAsync(userId);
                if (foundBooking == null)
                    throw new Exception($"BookingService: GetBookingsByUserIdAsync - no booking found for user {userId}");
                return MapToBookingProfileDto(foundBooking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: GetBookingsByUserIdAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: GetBookingsByUserIdAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<BookingProfileDataDto>?> GetBookingsByHospitalIdAsync(string hospitalId)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                var foundBookings = await _bookingRepository.GetBookingDetailsByHospitalUidAsync(hospitalId);
                if (foundBookings == null)
                    throw new Exception($"BookingService: GetBookingsByHospitalIdAsync - no bookings found for hospital {hospitalId}");

                List<BookingProfileDataDto> bookingList = [];
                foreach (var booking in foundBookings)
                    bookingList.Add(MapToBookingProfileDto(booking));
                return bookingList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: GetBookingsByHospitalIdAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: GetBookingsByHospitalIdAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<BookingProfileDataDto>?> GetAllBookingsAsync()
        {
            try
            {
                var foundBookings = await _bookingRepository.GetAllBookingDetailsAsync();
                if (foundBookings is null)
                    throw new Exception("BookingService: GetAllBookingsAsync - no bookings found");

                List<BookingProfileDataDto> bookingList = [];
                foreach (var booking in foundBookings)
                    bookingList.Add(MapToBookingProfileDto(booking));
                return bookingList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: GetAllBookingsAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: GetAllBookingsAsync - {ex.Message}", ex);
            }
        }

        public async Task DeleteBookingAsync(string bookingId)
        {
            ArgumentNullException.ThrowIfNull(bookingId);

            try
            {
                var foundBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookingId);
                if (foundBooking is null)
                    throw new Exception($"BookingService: DeleteBookingAsync - booking {bookingId} not found");

                foundBooking.IsDeleted = true;
                foundBooking.RemovedAt = DateTime.UtcNow;
                foundBooking.ModifiedAt = DateTime.UtcNow;
                await _bookingRepository.UpdateBookingAsync(foundBooking);

                // Restore Dose 1 slot only if it was not yet administered and not already canceled
                if (!string.IsNullOrEmpty(foundBooking.Dose1HospitalUid)
                    && !foundBooking.IsDose1Completed
                    && !foundBooking.IsD1RequestCanceled)
                    await _hospitalService.UpdateAvailableSlotsAsync(foundBooking.Dose1HospitalUid, 1);

                // Restore Dose 2 slot only if it was not yet administered and not already canceled
                if (!string.IsNullOrEmpty(foundBooking.Dose2HospitalUid)
                    && !foundBooking.IsDose2Completed
                    && !foundBooking.IsD2RequestCanceled)
                    await _hospitalService.UpdateAvailableSlotsAsync(foundBooking.Dose2HospitalUid, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: DeleteBookingAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: DeleteBookingAsync - {ex.Message}", ex);
            }
        }

        public async Task<bool> IsBookingExists(string bookingId)
        {
            ArgumentNullException.ThrowIfNull(bookingId);

            try
            {
                var booking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookingId);
                return booking != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: IsBookingExists - {Message}", ex.Message);
                throw new Exception($"BookingService: IsBookingExists - {ex.Message}", ex);
            }
        }

        // ── private mapping helpers ───────────────────────────────────────────────

        private async Task<BookingModel> MapCreateBookingRequestToBookingModel(CreateBookingRequestDto createBookingRequest)
        {
            var timestamp = DateTime.UtcNow;
            var guid = await _utilityService.GenerateGuidAsync();
            var uniqueId = await _utilityService.GenerateUniqueIdAsync($"Book_{createBookingRequest.UserUid}");

            return new BookingModel
            {
                BookingId = uniqueId,
                BookingUid = guid,
                UserUid = createBookingRequest.UserUid,
                Dose1RequestedDateTime = createBookingRequest.Dose1RequestedDateTime,
                Dose1SlotNumber = createBookingRequest.Dose1SlotNumber,
                Dose1HospitalUid = createBookingRequest.Dose1HospitalUid,
                IsDose1Completed = false,
                Dose1CompletedDateTime = null,
                Dose2RequestedDateTime = null,
                Dose2HospitalUid = "",
                Dose2SlotNumber = 0,
                IsDose2Completed = false,
                Dose2CompletedDateTime = null,
                IsVaccinationCompleted = false,
                VaccinationCompletedDateTime = null,
                IsD1RequestCanceled = false,
                IsD2RequestCanceled = false,
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
                IsDeleted = false
            };
        }

        private static BookingModel MapToDose2BookingModel(BookingModel existingBooking, BookDose2RequestDto bookDose2Request)
        {
            existingBooking.Dose2HospitalUid = bookDose2Request.Dose2HospitalUid;
            existingBooking.Dose2SlotNumber = bookDose2Request.Dose2SlotNumber;
            existingBooking.Dose2RequestedDateTime = bookDose2Request.Dose2RequestedDateTime;
            existingBooking.IsDose2Completed = false;
            existingBooking.ModifiedAt = DateTime.UtcNow;
            return existingBooking;
        }

        private static BookingModel MapUpdateBookingRequestToBookingModel(BookingModel foundBooking, UpdateBookingRequestDto updateBookingRequest)
        {
            foundBooking.Dose1RequestedDateTime = updateBookingRequest.Dose1RequestedDateTime;
            foundBooking.Dose1SlotNumber = updateBookingRequest.Dose1SlotNumber;
            foundBooking.Dose1HospitalUid = string.IsNullOrWhiteSpace(updateBookingRequest.Dose1HospitalUid)
                ? foundBooking.Dose1HospitalUid
                : updateBookingRequest.Dose1HospitalUid;
            foundBooking.IsDose1Completed = updateBookingRequest.IsDose1Completed;
            foundBooking.Dose1CompletedDateTime = updateBookingRequest.Dose1CompletedDateTime;

            foundBooking.Dose2RequestedDateTime = updateBookingRequest.Dose2RequestedDateTime;
            foundBooking.Dose2SlotNumber = updateBookingRequest.Dose2SlotNumber;
            foundBooking.Dose2HospitalUid = string.IsNullOrWhiteSpace(updateBookingRequest.Dose2HospitalUid)
                ? foundBooking.Dose2HospitalUid
                : updateBookingRequest.Dose2HospitalUid;
            foundBooking.IsDose2Completed = updateBookingRequest.IsDose2Completed;
            foundBooking.Dose2CompletedDateTime = updateBookingRequest.Dose2CompletedDateTime;

            foundBooking.IsVaccinationCompleted = updateBookingRequest.IsVaccinationCompleted;
            foundBooking.VaccinationCompletedDateTime = updateBookingRequest.VaccinationCompletedDateTime;
            foundBooking.IsD1RequestCanceled = updateBookingRequest.IsD1RequestCanceled;
            foundBooking.IsD2RequestCanceled = updateBookingRequest.IsD2RequestCanceled;
            foundBooking.ModifiedAt = DateTime.UtcNow;
            return foundBooking;
        }

        private static CreateBookingResponseDto MapToCreateBookingResponseDto(BookingModel booking)
        {
            return new CreateBookingResponseDto
            {
                BookingId = booking.BookingId,
                UserUid = booking.UserUid,
                Dose1RequestedDateTime = booking.Dose1RequestedDateTime,
                Dose1SlotNumber = booking.Dose1SlotNumber,
                Dose1HospitalUid = booking.Dose1HospitalUid
            };
        }

        private static UpdateBookingResponseDto MapToUpdateBookingResponseDto(BookingModel booking)
        {
            return new UpdateBookingResponseDto
            {
                BookingId = booking.BookingId,
                UserUid = booking.UserUid,
                Dose1RequestedDateTime = booking.Dose1RequestedDateTime,
                Dose1SlotNumber = booking.Dose1SlotNumber,
                Dose1HospitalUid = booking.Dose1HospitalUid,
                IsDose1Completed = booking.IsDose1Completed,
                Dose1CompletedDateTime = booking.Dose1CompletedDateTime,
                Dose2RequestedDateTime = booking.Dose2RequestedDateTime,
                Dose2SlotNumber = booking.Dose2SlotNumber,
                Dose2HospitalUid = booking.Dose2HospitalUid,
                IsDose2Completed = booking.IsDose2Completed,
                Dose2CompletedDateTime = booking.Dose2CompletedDateTime,
                IsVaccinationCompleted = booking.IsVaccinationCompleted,
                VaccinationCompletedDateTime = booking.VaccinationCompletedDateTime,
                IsD1RequestCanceled = booking.IsD1RequestCanceled,
                IsD2RequestCanceled = booking.IsD2RequestCanceled
            };
        }

        private static BookDose2ResponseDto MapToBookDose2ResponseDto(BookingModel booking)
        {
            return new BookDose2ResponseDto
            {
                BookingId = booking.BookingId,
                UserUid = booking.UserUid,
                Dose2HospitalUid = booking.Dose2HospitalUid,
                Dose2SlotNumber = booking.Dose2SlotNumber,
                Dose2RequestedDateTime = booking.Dose2RequestedDateTime,
                IsDose2Completed = booking.IsDose2Completed
            };
        }

        private static BookingProfileDataDto MapToBookingProfileDto(BookingModel booking)
        {
            return new BookingProfileDataDto
            {
                BookingId = booking.BookingId,
                UserUid = booking.UserUid,
                Dose1RequestedDateTime = booking.Dose1RequestedDateTime,
                Dose1SlotNumber = booking.Dose1SlotNumber,
                Dose1HospitalUid = booking.Dose1HospitalUid,
                IsDose1Completed = booking.IsDose1Completed,
                Dose1CompletedDateTime = booking.Dose1CompletedDateTime,
                Dose2RequestedDateTime = booking.Dose2RequestedDateTime,
                Dose2SlotNumber = booking.Dose2SlotNumber,
                Dose2HospitalUid = booking.Dose2HospitalUid,
                IsDose2Completed = booking.IsDose2Completed,
                Dose2CompletedDateTime = booking.Dose2CompletedDateTime,
                IsVaccinationCompleted = booking.IsVaccinationCompleted,
                VaccinationCompletedDateTime = booking.VaccinationCompletedDateTime,
                IsD1RequestCanceled = booking.IsD1RequestCanceled,
                IsD2RequestCanceled = booking.IsD2RequestCanceled,
                CreatedAt = booking.CreatedAt,
                ModifiedAt = booking.ModifiedAt
            };
        }
    }
}
