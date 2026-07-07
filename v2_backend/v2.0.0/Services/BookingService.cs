using Vaxtrack.Interfaces;
using Vaxtrack.Dtos.BookingDtos;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;
using Vaxtrack.Interfaces.UtilityInterfaces;

namespace Vaxtrack.Services
{
    public class BookingService : IBookingService
    {
        private const int WorkStartHour = 9;
        private const int SlotMinutes = 15;
        private const int SlotsPerDay = 32; // 9 AM - 5 PM

        private readonly IBookingRepository _bookingRepository;
        private readonly IUtilityService _utilityService;
        private readonly IHospitalService _hospitalService;
        private readonly IHospitalRepository _hospitalRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserRoleMappingRepository _roleMappingRepository;
        private readonly IUserCredentialsRepository _userCredentialsRepository;
        private readonly IBookingAuditLogRepository _bookingAuditLogRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            IBookingRepository bookingRepository,
            IUtilityService utilityService,
            IHospitalService hospitalService,
            IHospitalRepository hospitalRepository,
            IUserRepository userRepository,
            IUserRoleMappingRepository roleMappingRepository,
            IUserCredentialsRepository userCredentialsRepository,
            IBookingAuditLogRepository bookingAuditLogRepository,
            INotificationService notificationService,
            ILogger<BookingService> logger)
        {
            _bookingRepository = bookingRepository;
            _utilityService = utilityService;
            _hospitalService = hospitalService;
            _hospitalRepository = hospitalRepository;
            _userRepository = userRepository;
            _roleMappingRepository = roleMappingRepository;
            _userCredentialsRepository = userCredentialsRepository;
            _bookingAuditLogRepository = bookingAuditLogRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        // Booking.Dose1HospitalUid/Dose2HospitalUid actually store the hospital's readable
        // HospitalId (confirmed: CreateBookingAsync/BookDose2Async resolve hospitals and adjust
        // slots via HospitalId-keyed lookups) — a pre-existing naming inconsistency. Hospital-admin
        // role mappings, however, are always scoped by the true HospitalUid GUID. This helper
        // translates a booking's stored HospitalId to its real HospitalUid so authorization checks
        // compare like with like.
        private async Task<string> ResolveHospitalUidAsync(string hospitalId)
        {
            if (string.IsNullOrEmpty(hospitalId)) return "";
            var hospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);
            return hospital?.HospitalUid ?? "";
        }

        private async Task LogAuditAsync(string bookingId, int doseNumber, string actionType, string actorUserUid, string actorRole, string? comment)
        {
            await _bookingAuditLogRepository.CreateEntryAsync(new BookingAuditLogModel
            {
                BookingId = bookingId,
                DoseNumber = doseNumber,
                ActionType = actionType,
                ActorUserUid = actorUserUid,
                ActorRole = actorRole,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Platform-admin fallback check for Approve/Reject/Cancel scoping: a platform admin may act
        // directly on a hospital's bookings only when that hospital currently has no hospital-admin
        // assigned — mirrors HospitalService.AuthorizeUnregisterAsync's no-second-party fallback.
        private async Task<bool> HasHospitalAdminAssignedAsync(string hospitalUid)
        {
            var assignedAdmins = await _roleMappingRepository.GetRoleMappingsByRoleTagAsync("hospital-admin", hospitalUid);
            return assignedAdmins.Any(m => m.IsActive);
        }

        // Server-side per-calendar-day slot allocation: finds the latest booked time for the
        // given hospital+date and assigns the next 15-minute slot after it (9 AM, slot 1 if none
        // exist yet for that date). Throws if the day's ~32 slots (9 AM-5 PM) are exhausted.
        private async Task<(int SlotNumber, DateTime SlotDateTime)> AllocateNextSlotAsync(string hospitalId, DateTime requestedDate)
        {
            var dayStart = requestedDate.Date;
            var latest = await _bookingRepository.GetLatestSlotEndTimeForHospitalAndDateAsync(hospitalId, dayStart);

            DateTime nextSlotTime;
            int nextSlotNumber;

            if (latest is null)
            {
                nextSlotTime = dayStart.AddHours(WorkStartHour);
                nextSlotNumber = 1;
            }
            else
            {
                nextSlotTime = latest.Value.AddMinutes(SlotMinutes);
                nextSlotNumber = (int)((nextSlotTime - dayStart.AddHours(WorkStartHour)).TotalMinutes / SlotMinutes) + 1;
            }

            if (nextSlotNumber > SlotsPerDay)
                throw new Exception($"BookingService: AllocateNextSlotAsync - hospital {hospitalId} is fully booked on {dayStart:yyyy-MM-dd}; please choose another date");

            return (nextSlotNumber, nextSlotTime);
        }

        public async Task<CreateBookingResponseDto> CreateBookingAsync(CreateBookingRequestDto createBookingRequest, string callerUserUid)
        {
            ArgumentNullException.ThrowIfNull(createBookingRequest);

            try
            {
                // A user can only book for themselves — prevents impersonation
                if (createBookingRequest.UserUid != callerUserUid)
                    throw new UnauthorizedAccessException($"BookingService: CreateBookingAsync - caller cannot create a booking for another user");

                // Requested date must be in the future (calendar-day granularity — exact time is server-assigned)
                if (createBookingRequest.Dose1RequestedDateTime.Date <= DateTime.UtcNow.Date)
                    throw new Exception($"BookingService: CreateBookingAsync - Dose1RequestedDateTime must be a future date");

                // Enforce one active booking per user — duplicate bookings are not allowed
                bool userAlreadyBooked = await _bookingRepository.IsBookingExistsAsync(createBookingRequest.UserUid);
                if (userAlreadyBooked)
                    throw new Exception($"BookingService: CreateBookingAsync - user {createBookingRequest.UserUid} already has an active booking");

                // Validate the hospital has capacity before reserving a slot
                var hospital = await _hospitalService.GetHospitalByIdAsync(createBookingRequest.Dose1HospitalUid);
                if (hospital.SlotsAvailable <= 0)
                    throw new Exception($"BookingService: CreateBookingAsync - hospital {createBookingRequest.Dose1HospitalUid} has no available slots");

                var (slotNumber, slotDateTime) = await AllocateNextSlotAsync(createBookingRequest.Dose1HospitalUid, createBookingRequest.Dose1RequestedDateTime);

                var newBooking = await MapCreateBookingRequestToBookingModel(createBookingRequest, slotNumber, slotDateTime);
                var createdBooking = await _bookingRepository.CreateBookingAsync(newBooking);

                // Decrement available slot count by 1 after confirming the booking
                await _hospitalService.UpdateAvailableSlotsAsync(createBookingRequest.Dose1HospitalUid, -1);

                await LogAuditAsync(createdBooking.BookingId, 1, "Dose1Booked", callerUserUid, "user", null);

                return MapToCreateBookingResponseDto(createdBooking);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: CreateBookingAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: CreateBookingAsync - {ex.Message}", ex);
            }
        }

        public async Task<BookDose2ResponseDto> BookDose2Async(BookDose2RequestDto bookDose2Request, string callerUserUid)
        {
            ArgumentNullException.ThrowIfNull(bookDose2Request);

            try
            {
                // Requested date must be in the future (calendar-day granularity — exact time is server-assigned)
                if (bookDose2Request.Dose2RequestedDateTime.Date <= DateTime.UtcNow.Date)
                    throw new Exception($"BookingService: BookDose2Async - Dose2RequestedDateTime must be a future date");

                var foundBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookDose2Request.BookingId);

                if (foundBooking == null)
                    throw new Exception($"BookingService: BookDose2Async - booking {bookDose2Request.BookingId} not found");

                // Ownership check — validated against JWT sub claim, not the client-supplied DTO field
                if (foundBooking.UserUid != callerUserUid)
                    throw new UnauthorizedAccessException($"BookingService: BookDose2Async - caller does not own booking {bookDose2Request.BookingId}");

                // Dose 1 must be physically administered before Dose 2 can be scheduled
                if (!foundBooking.IsDose1Completed)
                    throw new Exception($"BookingService: BookDose2Async - Dose 1 must be completed before booking Dose 2 for booking {bookDose2Request.BookingId}");

                // Prevent re-booking Dose 2 if it is already scheduled and was not cancelled
                if (!string.IsNullOrEmpty(foundBooking.Dose2HospitalUid) && !foundBooking.IsD2RequestCanceled)
                    throw new Exception($"BookingService: BookDose2Async - Dose 2 is already booked for booking {bookDose2Request.BookingId}");

                if (foundBooking.IsDose2Completed)
                    throw new Exception($"BookingService: BookDose2Async - Dose 2 is already completed for booking {bookDose2Request.BookingId}");

                if (foundBooking.IsD1RequestCanceled)
                    throw new Exception($"BookingService: BookDose2Async - booking {bookDose2Request.BookingId} Dose 1 was canceled");

                // Validate the Dose 2 hospital has available slots
                var hospital = await _hospitalService.GetHospitalByIdAsync(bookDose2Request.Dose2HospitalUid);
                if (hospital.SlotsAvailable <= 0)
                    throw new Exception($"BookingService: BookDose2Async - hospital {bookDose2Request.Dose2HospitalUid} has no available slots");

                var (slotNumber, slotDateTime) = await AllocateNextSlotAsync(bookDose2Request.Dose2HospitalUid, bookDose2Request.Dose2RequestedDateTime);

                var mappedBooking = MapToDose2BookingModel(foundBooking, bookDose2Request, slotNumber, slotDateTime);
                var updatedBooking = await _bookingRepository.UpdateBookingAsync(mappedBooking);

                // Decrement Dose 2 hospital slot count by 1
                await _hospitalService.UpdateAvailableSlotsAsync(bookDose2Request.Dose2HospitalUid, -1);

                await LogAuditAsync(updatedBooking.BookingId, 2, "Dose2Booked", callerUserUid, "user", null);

                return MapToBookDose2ResponseDto(updatedBooking);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: BookDose2Async - {Message}", ex.Message);
                throw new Exception($"BookingService: BookDose2Async - {ex.Message}", ex);
            }
        }

        public async Task<BookingProfileDataDto> RebookDose1Async(RebookDose1RequestDto request, string callerUserUid)
        {
            /*
             * Rebook Logic (Dose 1):
             * ----------------------
             * Allows the booking owner to reschedule a Dose 1 that was previously cancelled
             * (either self-cancelled or admin-rejected). The old hospital slot was already
             * restored (+1) at cancellation time, so we only charge the new hospital (-1).
             *
             * Edge cases blocked:
             *   - Caller is not the booking owner        → UnauthorizedAccessException
             *   - Dose 1 is not in cancelled state       → throws
             *   - Dose 1 is already completed            → throws (should never reach here)
             *   - New requested date is not in future    → throws
             *   - New hospital has no available slots    → throws
             */

            ArgumentNullException.ThrowIfNull(request);

            try
            {
                if (request.UserUid != callerUserUid)
                    throw new UnauthorizedAccessException("BookingService: RebookDose1Async - caller cannot rebook another user's booking");

                var booking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(request.BookingId);
                if (booking == null)
                    throw new Exception($"BookingService: RebookDose1Async - booking {request.BookingId} not found");

                if (booking.UserUid != callerUserUid)
                    throw new UnauthorizedAccessException("BookingService: RebookDose1Async - caller does not own this booking");

                if (!booking.IsD1RequestCanceled)
                    throw new Exception("BookingService: RebookDose1Async - dose 1 is not cancelled; use EditBookingAsync to change a pending booking");

                if (booking.IsDose1Completed)
                    throw new Exception("BookingService: RebookDose1Async - dose 1 is already completed and cannot be rebooked");

                // Requested date must be in the future (calendar-day granularity — exact time is server-assigned)
                if (request.NewRequestedDateTime.Date <= DateTime.UtcNow.Date)
                    throw new Exception("BookingService: RebookDose1Async - requested date must be in the future");

                var newHospital = await _hospitalService.GetHospitalByIdAsync(request.NewHospitalUid);
                if (newHospital.SlotsAvailable <= 0)
                    throw new Exception($"BookingService: RebookDose1Async - hospital {request.NewHospitalUid} has no available slots");

                var (slotNumber, slotDateTime) = await AllocateNextSlotAsync(request.NewHospitalUid, request.NewRequestedDateTime);

                // Take slot at new hospital — old slot was already released at cancellation time
                await _hospitalService.UpdateAvailableSlotsAsync(request.NewHospitalUid, -1);

                booking.Dose1HospitalUid = request.NewHospitalUid;
                booking.Dose1SlotNumber  = slotNumber;
                booking.Dose1RequestedDateTime = slotDateTime;
                booking.IsD1RequestCanceled = false;
                booking.IsD1RejectedByAdmin = false;
                booking.ModifiedAt = DateTime.UtcNow;

                var updated = await _bookingRepository.UpdateBookingAsync(booking);

                await LogAuditAsync(updated.BookingId, 1, "Rebooked", callerUserUid, "user", null);

                return await MapToBookingProfileDtoAsync(updated);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: RebookDose1Async - {Message}", ex.Message);
                throw new Exception($"BookingService: RebookDose1Async - {ex.Message}", ex);
            }
        }

        public async Task<BookingProfileDataDto> EditBookingAsync(EditBookingRequestDto request, string callerUserUid)
        {
            /*
             * Edit Logic:
             * -----------
             * Allows the booking owner to change the hospital and/or scheduled date for a
             * pending dose — i.e., one that has not yet been approved or cancelled.
             *
             * Slot management:
             *   - Same hospital: the slot is already reserved; only the date is updated.
             *     No slot counts change.
             *   - Different hospital: the old hospital's slot is restored (+1) and the new
             *     hospital's slot count is decremented (-1). The slot number is updated to
             *     the value computed by the frontend from the new hospital's availability.
             *
             * Edge cases blocked:
             *   - Caller is not the booking owner           → UnauthorizedAccessException
             *   - Requested date is not in the future       → throws
             *   - Dose 1 already completed or cancelled     → throws (for DoseNumber == 1)
             *   - Dose 2 not yet booked                     → throws (for DoseNumber == 2)
             *   - Dose 2 already completed or cancelled     → throws (for DoseNumber == 2)
             *   - New hospital has no available slots       → throws (only on hospital change)
             */

            ArgumentNullException.ThrowIfNull(request);

            try
            {
                if (request.UserUid != callerUserUid)
                    throw new UnauthorizedAccessException("BookingService: EditBookingAsync - caller cannot edit another user's booking");

                var booking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(request.BookingId);
                if (booking == null)
                    throw new Exception($"BookingService: EditBookingAsync - booking {request.BookingId} not found");

                if (booking.UserUid != callerUserUid)
                    throw new UnauthorizedAccessException("BookingService: EditBookingAsync - caller does not own this booking");

                // Requested date must be in the future (calendar-day granularity — exact time is server-assigned)
                if (request.NewRequestedDateTime.Date <= DateTime.UtcNow.Date)
                    throw new Exception("BookingService: EditBookingAsync - requested date must be in the future");

                var timestamp = DateTime.UtcNow;

                if (request.DoseNumber == 1)
                {
                    if (booking.IsDose1Completed)
                        throw new Exception("BookingService: EditBookingAsync - dose 1 is already completed and cannot be edited");
                    if (booking.IsD1RequestCanceled)
                        throw new Exception("BookingService: EditBookingAsync - dose 1 is cancelled and cannot be edited");

                    bool hospitalChanged = booking.Dose1HospitalUid != request.NewHospitalUid;
                    bool dateChanged = booking.Dose1RequestedDateTime.Date != request.NewRequestedDateTime.Date;

                    if (hospitalChanged)
                    {
                        var newHospital = await _hospitalService.GetHospitalByIdAsync(request.NewHospitalUid);
                        if (newHospital.SlotsAvailable <= 0)
                            throw new Exception($"BookingService: EditBookingAsync - hospital {request.NewHospitalUid} has no available slots");
                    }

                    if (hospitalChanged || dateChanged)
                    {
                        var effectiveHospitalId = hospitalChanged ? request.NewHospitalUid : booking.Dose1HospitalUid;
                        var (slotNumber, slotDateTime) = await AllocateNextSlotAsync(effectiveHospitalId, request.NewRequestedDateTime);

                        if (hospitalChanged)
                        {
                            await _hospitalService.UpdateAvailableSlotsAsync(booking.Dose1HospitalUid, 1);
                            await _hospitalService.UpdateAvailableSlotsAsync(request.NewHospitalUid, -1);
                            booking.Dose1HospitalUid = request.NewHospitalUid;
                        }

                        booking.Dose1SlotNumber = slotNumber;
                        booking.Dose1RequestedDateTime = slotDateTime;
                    }
                }
                else if (request.DoseNumber == 2)
                {
                    if (!booking.IsDose1Completed)
                        throw new Exception("BookingService: EditBookingAsync - dose 1 must be completed before editing dose 2");
                    if (string.IsNullOrEmpty(booking.Dose2HospitalUid))
                        throw new Exception("BookingService: EditBookingAsync - dose 2 has not been booked yet");
                    if (booking.IsDose2Completed)
                        throw new Exception("BookingService: EditBookingAsync - dose 2 is already completed and cannot be edited");
                    if (booking.IsD2RequestCanceled)
                        throw new Exception("BookingService: EditBookingAsync - dose 2 is cancelled and cannot be edited");

                    bool hospitalChanged = booking.Dose2HospitalUid != request.NewHospitalUid;
                    bool dateChanged = (booking.Dose2RequestedDateTime?.Date) != request.NewRequestedDateTime.Date;

                    if (hospitalChanged)
                    {
                        var newHospital = await _hospitalService.GetHospitalByIdAsync(request.NewHospitalUid);
                        if (newHospital.SlotsAvailable <= 0)
                            throw new Exception($"BookingService: EditBookingAsync - hospital {request.NewHospitalUid} has no available slots");
                    }

                    if (hospitalChanged || dateChanged)
                    {
                        var effectiveHospitalId = hospitalChanged ? request.NewHospitalUid : booking.Dose2HospitalUid;
                        var (slotNumber, slotDateTime) = await AllocateNextSlotAsync(effectiveHospitalId, request.NewRequestedDateTime);

                        if (hospitalChanged)
                        {
                            await _hospitalService.UpdateAvailableSlotsAsync(booking.Dose2HospitalUid, 1);
                            await _hospitalService.UpdateAvailableSlotsAsync(request.NewHospitalUid, -1);
                            booking.Dose2HospitalUid = request.NewHospitalUid;
                        }

                        booking.Dose2SlotNumber = slotNumber;
                        booking.Dose2RequestedDateTime = slotDateTime;
                    }
                }
                else
                {
                    throw new Exception($"BookingService: EditBookingAsync - invalid dose number {request.DoseNumber}");
                }

                booking.ModifiedAt = timestamp;
                var updated = await _bookingRepository.UpdateBookingAsync(booking);

                await LogAuditAsync(updated.BookingId, request.DoseNumber, "Edited", callerUserUid, "user", null);

                return await MapToBookingProfileDtoAsync(updated);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: EditBookingAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: EditBookingAsync - {ex.Message}", ex);
            }
        }

        public async Task<BookingProfileDataDto> ApproveBookingsAsync(string bookingId, string callerUserUid, bool callerIsAdmin, string? comment)
        {
            /*
             * Approve Logic:
             * --------------
             * "Approve" means a hospital staff member confirms that a dose was physically
             * administered to the user.
             *
             * Authorization:
             *   - Platform admin: can approve any booking.
             *   - Hospital-admin: can approve bookings only at the hospital they manage
             *     (ContextId = HospitalId in UserRoleMappings). The check is scoped to
             *     whichever hospital is handling the pending dose — Dose 1 or Dose 2.
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
             *   - Caller not admin and not hospital-admin      → throws UnauthorizedAccessException
             *   - Dose 1 canceled and no Dose 2 pending       → throws (nothing to approve)
             *   - Dose 1 complete but Dose 2 not yet booked   → throws (nothing to approve)
             *   - Dose 2 already canceled or completed        → throws (nothing to approve)
             */

            ArgumentNullException.ThrowIfNull(bookingId);

            try
            {
                var foundBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookingId);

                if (foundBooking == null)
                    throw new Exception($"BookingService: ApproveBookingsAsync - booking {bookingId} not found");

                if (foundBooking.IsVaccinationCompleted)
                    throw new Exception($"BookingService: ApproveBookingsAsync - booking {bookingId} vaccination is already fully completed");

                // Determine which dose is approvable before running the auth check,
                // so the scoped hospital-admin check targets the right hospital.
                bool approvingDose1 = !foundBooking.IsDose1Completed && !foundBooking.IsD1RequestCanceled;
                bool approvingDose2 = foundBooking.IsDose1Completed
                    && !string.IsNullOrEmpty(foundBooking.Dose2HospitalUid)
                    && !foundBooking.IsDose2Completed
                    && !foundBooking.IsD2RequestCanceled;

                if (!approvingDose1 && !approvingDose2)
                    throw new Exception($"BookingService: ApproveBookingsAsync - booking {bookingId} has no pending dose to approve");

                // Hospital-admin check — scoped to the hospital handling the pending dose.
                // Platform admin bypasses this only if the hospital currently has no hospital-admin
                // assigned (same no-second-party fallback as HospitalService.AuthorizeUnregisterAsync);
                // otherwise the relevant hospital's own hospital-admin must act.
                string relevantHospitalIdForApproval = approvingDose1
                    ? foundBooking.Dose1HospitalUid
                    : foundBooking.Dose2HospitalUid;
                string relevantHospitalUidForApproval = await ResolveHospitalUidAsync(relevantHospitalIdForApproval);

                bool isHospitalAdminForApproval = await _roleMappingRepository.IsUserInRoleAsync(
                    callerUserUid, "hospital-admin", relevantHospitalUidForApproval);

                string actorRole;
                if (isHospitalAdminForApproval)
                {
                    actorRole = "hospital-admin";
                }
                else if (callerIsAdmin && !await HasHospitalAdminAssignedAsync(relevantHospitalUidForApproval))
                {
                    actorRole = "admin";
                }
                else
                {
                    throw new UnauthorizedAccessException(
                        $"BookingService: ApproveBookingsAsync - caller is not authorized to approve bookings at hospital {relevantHospitalIdForApproval}");
                }

                var timestamp = DateTime.UtcNow;
                int approvedDoseNumber;

                if (approvingDose1)
                {
                    foundBooking.IsDose1Completed = true;
                    foundBooking.Dose1CompletedDateTime = timestamp;
                    approvedDoseNumber = 1;
                }
                else
                {
                    foundBooking.IsDose2Completed = true;
                    foundBooking.Dose2CompletedDateTime = timestamp;
                    foundBooking.IsVaccinationCompleted = true;
                    foundBooking.VaccinationCompletedDateTime = timestamp;
                    approvedDoseNumber = 2;
                }

                foundBooking.ModifiedAt = timestamp;
                var updatedBooking = await _bookingRepository.UpdateBookingAsync(foundBooking);

                await LogAuditAsync(updatedBooking.BookingId, approvedDoseNumber, "Approved", callerUserUid, actorRole, comment);
                await _notificationService.NotifyAsync(updatedBooking.UserUid, $"Your Dose {approvedDoseNumber} booking has been approved.", "/booking");

                return await MapToBookingProfileDtoAsync(updatedBooking);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: ApproveBookingsAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: ApproveBookingsAsync - {ex.Message}", ex);
            }
        }

        public async Task<BookingProfileDataDto> CancelBookingsAsync(string bookingId, string callerUserUid, bool callerIsAdmin, string? comment)
        {
            /*
             * Cancel Logic:
             * -------------
             * "Cancel" means a user or admin withdraws a pending dose request before it is administered.
             * Canceling restores the hospital's available slot so another user can book it.
             * Ownership: only the booking owner or an admin can cancel.
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
             *   - Caller not owner (non-admin)                              → throws UnauthorizedAccessException
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

                bool cancelingDose1 = !foundBooking.IsDose1Completed && !foundBooking.IsD1RequestCanceled;
                bool cancelingDose2 = foundBooking.IsDose1Completed
                    && !string.IsNullOrEmpty(foundBooking.Dose2HospitalUid)
                    && !foundBooking.IsDose2Completed
                    && !foundBooking.IsD2RequestCanceled;

                if (!cancelingDose1 && !cancelingDose2)
                    throw new Exception($"BookingService: CancelBookingsAsync - booking {bookingId} has no active dose request to cancel");

                // Ownership: the booking owner may always cancel their own pending dose. Otherwise,
                // the hospital-admin scoped to whichever hospital owns that dose may cancel it too
                // (mirrors Approve/Reject scoping); platform admin bypasses this only when that
                // hospital currently has no hospital-admin assigned (same fallback as Approve/Reject).
                bool isOwner = foundBooking.UserUid == callerUserUid;
                string actorRole = "user";

                if (!isOwner)
                {
                    string relevantHospitalIdForCancel = cancelingDose1
                        ? foundBooking.Dose1HospitalUid
                        : foundBooking.Dose2HospitalUid;
                    string relevantHospitalUidForCancel = await ResolveHospitalUidAsync(relevantHospitalIdForCancel);

                    bool isHospitalAdminForCancel = await _roleMappingRepository.IsUserInRoleAsync(
                        callerUserUid, "hospital-admin", relevantHospitalUidForCancel);

                    if (isHospitalAdminForCancel)
                    {
                        actorRole = "hospital-admin";
                    }
                    else if (callerIsAdmin && !await HasHospitalAdminAssignedAsync(relevantHospitalUidForCancel))
                    {
                        actorRole = "admin";
                    }
                    else
                    {
                        throw new UnauthorizedAccessException(
                            $"BookingService: CancelBookingsAsync - caller is not authorized to cancel this booking");
                    }
                }

                var timestamp = DateTime.UtcNow;
                int canceledDoseNumber;

                if (cancelingDose1)
                {
                    // Cancel Dose 1 and free up the reserved hospital slot
                    foundBooking.IsD1RequestCanceled = true;
                    foundBooking.IsD1RejectedByAdmin = false; // self-cancel is distinct from admin-reject
                    foundBooking.ModifiedAt = timestamp;
                    await _bookingRepository.UpdateBookingAsync(foundBooking);
                    await _hospitalService.UpdateAvailableSlotsAsync(foundBooking.Dose1HospitalUid, 1);
                    canceledDoseNumber = 1;
                }
                else
                {
                    // Cancel Dose 2 and free up the reserved hospital slot
                    foundBooking.IsD2RequestCanceled = true;
                    foundBooking.IsD2RejectedByAdmin = false; // self-cancel is distinct from admin-reject
                    foundBooking.ModifiedAt = timestamp;
                    await _bookingRepository.UpdateBookingAsync(foundBooking);
                    await _hospitalService.UpdateAvailableSlotsAsync(foundBooking.Dose2HospitalUid, 1);
                    canceledDoseNumber = 2;
                }

                var updatedBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookingId);

                await LogAuditAsync(bookingId, canceledDoseNumber, "Cancelled", callerUserUid, actorRole, comment);

                // Only notify if someone else acted on this booking — no need to tell a user
                // about their own self-cancellation.
                if (updatedBooking!.UserUid != callerUserUid)
                    await _notificationService.NotifyAsync(updatedBooking.UserUid, $"Your Dose {canceledDoseNumber} booking has been cancelled.", "/booking");

                return await MapToBookingProfileDtoAsync(updatedBooking!);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: CancelBookingsAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: CancelBookingsAsync - {ex.Message}", ex);
            }
        }

        public async Task<BookingProfileDataDto> RejectBookingAsync(string bookingId, string callerUserUid, bool callerIsAdmin, string? comment)
        {
            /*
             * Reject Logic:
             * -------------
             * Distinct from Cancel: only an admin/hospital-admin can reject (a plain owner
             * cancels, they cannot reject their own booking). Semantically the hospital
             * declined the request. Sets IsD1RequestCanceled (same "no longer actionable"
             * flag Approve/Cancel already branch on) AND IsD1RejectedByAdmin, restores the
             * hospital slot exactly as Cancel does, and logs a distinct "Rejected" audit entry.
             */

            ArgumentNullException.ThrowIfNull(bookingId);

            try
            {
                var foundBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookingId);

                if (foundBooking == null)
                    throw new Exception($"BookingService: RejectBookingAsync - booking {bookingId} not found");

                if (foundBooking.IsVaccinationCompleted)
                    throw new Exception($"BookingService: RejectBookingAsync - booking {bookingId} vaccination is already completed and cannot be rejected");

                bool rejectingDose1 = !foundBooking.IsDose1Completed && !foundBooking.IsD1RequestCanceled;
                bool rejectingDose2 = foundBooking.IsDose1Completed
                    && !string.IsNullOrEmpty(foundBooking.Dose2HospitalUid)
                    && !foundBooking.IsDose2Completed
                    && !foundBooking.IsD2RequestCanceled;

                if (!rejectingDose1 && !rejectingDose2)
                    throw new Exception($"BookingService: RejectBookingAsync - booking {bookingId} has no active dose request to reject");

                // Only admin or hospital-admin (scoped to the relevant hospital) may reject — a plain
                // owner cannot. Platform admin bypasses the hospital scoping only when that hospital
                // currently has no hospital-admin assigned (same fallback as Approve).
                string relevantHospitalIdForRejection = rejectingDose1
                    ? foundBooking.Dose1HospitalUid
                    : foundBooking.Dose2HospitalUid;
                string relevantHospitalUidForRejection = await ResolveHospitalUidAsync(relevantHospitalIdForRejection);

                bool isHospitalAdminForRejection = await _roleMappingRepository.IsUserInRoleAsync(
                    callerUserUid, "hospital-admin", relevantHospitalUidForRejection);

                string actorRole;
                if (isHospitalAdminForRejection)
                {
                    actorRole = "hospital-admin";
                }
                else if (callerIsAdmin && !await HasHospitalAdminAssignedAsync(relevantHospitalUidForRejection))
                {
                    actorRole = "admin";
                }
                else
                {
                    throw new UnauthorizedAccessException(
                        $"BookingService: RejectBookingAsync - caller is not authorized to reject bookings at hospital {relevantHospitalIdForRejection}");
                }

                var timestamp = DateTime.UtcNow;
                int rejectedDoseNumber;

                if (rejectingDose1)
                {
                    foundBooking.IsD1RequestCanceled = true;
                    foundBooking.IsD1RejectedByAdmin = true;
                    foundBooking.ModifiedAt = timestamp;
                    await _bookingRepository.UpdateBookingAsync(foundBooking);
                    await _hospitalService.UpdateAvailableSlotsAsync(foundBooking.Dose1HospitalUid, 1);
                    rejectedDoseNumber = 1;
                }
                else
                {
                    foundBooking.IsD2RequestCanceled = true;
                    foundBooking.IsD2RejectedByAdmin = true;
                    foundBooking.ModifiedAt = timestamp;
                    await _bookingRepository.UpdateBookingAsync(foundBooking);
                    await _hospitalService.UpdateAvailableSlotsAsync(foundBooking.Dose2HospitalUid, 1);
                    rejectedDoseNumber = 2;
                }

                var updatedBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookingId);

                await LogAuditAsync(bookingId, rejectedDoseNumber, "Rejected", callerUserUid, actorRole, comment);
                await _notificationService.NotifyAsync(updatedBooking!.UserUid, $"Your Dose {rejectedDoseNumber} booking has been rejected.{(string.IsNullOrWhiteSpace(comment) ? "" : $" Reason: {comment}")}", "/booking");

                return await MapToBookingProfileDtoAsync(updatedBooking!);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: RejectBookingAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: RejectBookingAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<BookingAuditLogDto>> GetBookingAuditTrailAsync(string bookingId, string callerUserUid, bool callerIsAdmin)
        {
            ArgumentNullException.ThrowIfNull(bookingId);

            try
            {
                var foundBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookingId);
                if (foundBooking == null)
                    throw new Exception($"BookingService: GetBookingAuditTrailAsync - booking {bookingId} not found");

                if (!callerIsAdmin && foundBooking.UserUid != callerUserUid)
                    throw new UnauthorizedAccessException($"BookingService: GetBookingAuditTrailAsync - caller does not own booking {bookingId}");

                var entries = await _bookingAuditLogRepository.GetEntriesByBookingIdAsync(bookingId);
                return entries.Select(e => new BookingAuditLogDto
                {
                    BookingId = e.BookingId,
                    DoseNumber = e.DoseNumber,
                    ActionType = e.ActionType,
                    ActorUserUid = e.ActorUserUid,
                    ActorRole = e.ActorRole,
                    Comment = e.Comment,
                    CreatedAt = e.CreatedAt
                }).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: GetBookingAuditTrailAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: GetBookingAuditTrailAsync - {ex.Message}", ex);
            }
        }

        public async Task<CertificateDto> GetCertificateAsync(string bookingId)
        {
            /*
             * Public Certificate Logic:
             * --------------------------
             * Unauthenticated (AllowAnonymous) — anyone holding the bookingId/link can view or
             * verify a completed vaccination, mirroring how a real certificate's QR code works.
             * Deliberately exposes only certificate-relevant fields (name, age, gender, dose
             * dates, hospital names) — never email/phone/address.
             *
             * Edge cases blocked:
             *   - Booking not found                  → throws.
             *   - Vaccination not yet fully completed → throws (nothing to certify yet).
             */

            ArgumentNullException.ThrowIfNull(bookingId);

            try
            {
                var foundBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookingId);
                if (foundBooking == null)
                    throw new Exception($"BookingService: GetCertificateAsync - booking {bookingId} not found");

                if (!foundBooking.IsVaccinationCompleted)
                    throw new Exception($"BookingService: GetCertificateAsync - booking {bookingId} vaccination is not yet fully completed");

                var user = await _userRepository.GetUserDetailsByUserUidAsync(foundBooking.UserUid);
                var dose1Hospital = await _hospitalRepository.GetHospitalByIdAsync(foundBooking.Dose1HospitalUid);
                var dose2Hospital = string.IsNullOrEmpty(foundBooking.Dose2HospitalUid)
                    ? null
                    : await _hospitalRepository.GetHospitalByIdAsync(foundBooking.Dose2HospitalUid);

                return new CertificateDto
                {
                    BookingId = foundBooking.BookingId,
                    BeneficiaryName = user?.UserName ?? "",
                    BeneficiaryAge = user?.UserAge ?? 0,
                    BeneficiaryGender = user?.UserGender ?? "",
                    Dose1HospitalName = dose1Hospital?.HospitalName ?? "",
                    Dose1CompletedDate = foundBooking.Dose1CompletedDateTime,
                    Dose2HospitalName = dose2Hospital?.HospitalName ?? "",
                    Dose2CompletedDate = foundBooking.Dose2CompletedDateTime,
                    VaccinationCompletedDate = foundBooking.VaccinationCompletedDateTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: GetCertificateAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: GetCertificateAsync - {ex.Message}", ex);
            }
        }

        public async Task<Dictionary<string, string>> GetVaccinationStatusesByUserUidsAsync(List<string> userUids)
        {
            ArgumentNullException.ThrowIfNull(userUids);

            try
            {
                var uidSet = userUids.ToHashSet();
                var allBookings = await _bookingRepository.GetAllBookingDetailsAsync() ?? [];
                return allBookings
                    .Where(b => uidSet.Contains(b.UserUid))
                    .ToDictionary(b => b.UserUid, b => ComputeVaccinationDisplayStatus(b));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: GetVaccinationStatusesByUserUidsAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: GetVaccinationStatusesByUserUidsAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<BookingProfileDataDto>> GetActionableBookingsAsync(string callerUserUid, bool callerIsAdmin)
        {
            try
            {
                // Booking rows store hospitals by HospitalId, but hospital-admin role mappings are
                // scoped by HospitalUid — fetch all hospitals once to translate between the two.
                var allHospitals = await _hospitalRepository.GetAllHospitalDetailsAsync() ?? [];
                var hospitalIdByUid = allHospitals.ToDictionary(h => h.HospitalUid, h => h.HospitalId);
                var hospitalUidById = allHospitals.ToDictionary(h => h.HospitalId, h => h.HospitalUid);

                List<BookingModel> candidates;
                HashSet<string>? hospitalUidsWithAdmin = null;

                if (callerIsAdmin)
                {
                    // Platform admin's list is scoped down to only bookings whose actionable dose
                    // belongs to a hospital with NO hospital-admin currently assigned — matches the
                    // Approve/Reject/Cancel fallback rule, so the "Bookings Management" tab only ever
                    // shows platform admin bookings they're actually allowed to act on.
                    candidates = await _bookingRepository.GetAllBookingDetailsAsync() ?? [];
                    var allHospitalAdminMappings = await _roleMappingRepository.GetRoleMappingsByRoleTagAsync("hospital-admin", "");
                    hospitalUidsWithAdmin = allHospitalAdminMappings.Select(m => m.ContextId).ToHashSet();
                }
                else
                {
                    var mappings = await _roleMappingRepository.GetRoleMappingsByUserUidAsync(callerUserUid);
                    var myHospitalUids = mappings
                        .Where(m => m.RoleTag == "hospital-admin" && m.IsActive)
                        .Select(m => m.ContextId)
                        .Distinct()
                        .ToList();

                    // Translate this admin's HospitalUids to the HospitalIds actually stored on bookings
                    var hospitalIds = myHospitalUids
                        .Where(hospitalIdByUid.ContainsKey)
                        .Select(uid => hospitalIdByUid[uid])
                        .ToList();

                    var seen = new Dictionary<string, BookingModel>();
                    foreach (var hospitalId in hospitalIds)
                    {
                        var bookings = await _bookingRepository.GetBookingDetailsByHospitalUidAsync(hospitalId);
                        if (bookings == null) continue;
                        foreach (var booking in bookings)
                            seen[booking.BookingId] = booking;
                    }
                    candidates = seen.Values.ToList();
                }

                var actionable = candidates.Where(IsActionable).ToList();

                if (hospitalUidsWithAdmin is not null)
                    actionable = actionable.Where(b =>
                    {
                        var relevantHospitalId = RelevantHospitalUidForAction(b);
                        var relevantHospitalUid = hospitalUidById.GetValueOrDefault(relevantHospitalId, "");
                        return !hospitalUidsWithAdmin.Contains(relevantHospitalUid);
                    }).ToList();

                List<BookingProfileDataDto> actionableDtos = [];
                foreach (var booking in actionable)
                    actionableDtos.Add(await MapToBookingProfileDtoAsync(booking));
                return actionableDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: GetActionableBookingsAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: GetActionableBookingsAsync - {ex.Message}", ex);
            }
        }

        public async Task<NextAvailableSlotResponseDto> GetNextAvailableSlotAsync(string hospitalId, DateTime date)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                var (slotNumber, slotDateTime) = await AllocateNextSlotAsync(hospitalId, date);
                return new NextAvailableSlotResponseDto { SlotNumber = slotNumber, SlotDateTime = slotDateTime };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: GetNextAvailableSlotAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: GetNextAvailableSlotAsync - {ex.Message}", ex);
            }
        }

        // Mirrors ApproveBookingsAsync's approvingDose1/approvingDose2 branching — a booking is
        // "actionable" if either dose still has a pending request awaiting approval/reject/cancel.
        private static bool IsActionable(BookingModel booking)
        {
            bool dose1Actionable = !booking.IsDose1Completed && !booking.IsD1RequestCanceled;
            bool dose2Actionable = booking.IsDose1Completed
                && !string.IsNullOrEmpty(booking.Dose2HospitalUid)
                && !booking.IsDose2Completed
                && !booking.IsD2RequestCanceled;
            return dose1Actionable || dose2Actionable;
        }

        // Same dose1-first priority as Approve/Reject/Cancel — identifies which hospital "owns"
        // a booking's currently-pending action, for fallback/scoping checks.
        private static string RelevantHospitalUidForAction(BookingModel booking)
        {
            bool dose1Actionable = !booking.IsDose1Completed && !booking.IsD1RequestCanceled;
            return dose1Actionable ? booking.Dose1HospitalUid : booking.Dose2HospitalUid;
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

                // Ownership check — UserUid in request must match the booking record
                if (foundBooking.UserUid != updateBookingRequest.UserUid)
                    throw new Exception($"BookingService: UpdateBookingAsync - booking {updateBookingRequest.BookingId} does not belong to user {updateBookingRequest.UserUid}");

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

        public async Task<BookingProfileDataDto?> GetBookingByBookingIdAsync(string bookingId, string callerUserUid, bool callerIsAdmin)
        {
            ArgumentNullException.ThrowIfNull(bookingId);

            try
            {
                var foundBooking = await _bookingRepository.GetBookingDetailsByBookingIdAsync(bookingId);
                if (foundBooking == null)
                    throw new Exception($"BookingService: GetBookingByBookingIdAsync - booking {bookingId} not found");

                if (!callerIsAdmin && foundBooking.UserUid != callerUserUid)
                    throw new UnauthorizedAccessException($"BookingService: GetBookingByBookingIdAsync - caller does not own booking {bookingId}");

                return await MapToBookingProfileDtoAsync(foundBooking);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: GetBookingByBookingIdAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: GetBookingByBookingIdAsync - {ex.Message}", ex);
            }
        }

        public async Task<BookingProfileDataDto?> GetBookingsByUserIdAsync(string userId, string callerUserUid, bool callerIsAdmin)
        {
            /*
             * Note: the 'userId' parameter is the user's UserUid (GUID), not the readable UserId.
             * This matches what the repository's GetBookingDetailsByUserUidAsync expects.
             * Ownership check: callerUserUid (from JWT) must match the requested userId.
             */

            ArgumentNullException.ThrowIfNull(userId);

            try
            {
                if (!callerIsAdmin && userId != callerUserUid)
                    throw new UnauthorizedAccessException($"BookingService: GetBookingsByUserIdAsync - caller cannot view another user's bookings");

                var foundBooking = await _bookingRepository.GetBookingDetailsByUserUidAsync(userId);
                if (foundBooking == null)
                    throw new Exception($"BookingService: GetBookingsByUserIdAsync - no booking found for user {userId}");
                return await MapToBookingProfileDtoAsync(foundBooking);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
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

                List<BookingProfileDataDto> bookingList = [];
                if (foundBookings is not null)
                    foreach (var booking in foundBookings)
                        bookingList.Add(await MapToBookingProfileDtoAsync(booking));
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

                List<BookingProfileDataDto> bookingList = [];
                if (foundBookings is not null)
                    foreach (var booking in foundBookings)
                        bookingList.Add(await MapToBookingProfileDtoAsync(booking));
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

        public async Task DeleteBookingsByUserUidAsync(string userUid)
        {
            /*
             * Cascade Delete Logic (called by UserService on user soft-deletion):
             * -------------------------------------------------------------------
             * Soft-deletes every non-deleted booking for a given UserUid and restores
             * any hospital slots that were still being held for pending doses.
             * Delegates to DeleteBookingAsync so the slot-restoration logic is not duplicated.
             *
             * If a booking is already fully vaccinated or canceled, DeleteBookingAsync still
             * soft-deletes it but correctly skips the slot restoration for completed/canceled doses.
             */

            ArgumentNullException.ThrowIfNull(userUid);

            try
            {
                var activeBookings = await _bookingRepository.GetAllActiveBookingsByUserUidAsync(userUid);
                foreach (var booking in activeBookings)
                    await DeleteBookingAsync(booking.BookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingService: DeleteBookingsByUserUidAsync - {Message}", ex.Message);
                throw new Exception($"BookingService: DeleteBookingsByUserUidAsync - {ex.Message}", ex);
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

        private async Task<BookingModel> MapCreateBookingRequestToBookingModel(CreateBookingRequestDto createBookingRequest, int slotNumber, DateTime slotDateTime)
        {
            var timestamp = DateTime.UtcNow;
            var guid = await _utilityService.GenerateGuidAsync();
            var uniqueId = await _utilityService.GenerateUniqueIdAsync($"Book_{createBookingRequest.UserUid}");

            return new BookingModel
            {
                BookingId = uniqueId,
                BookingUid = guid,
                UserUid = createBookingRequest.UserUid,
                Dose1RequestedDateTime = slotDateTime,
                Dose1SlotNumber = slotNumber,
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

        private static BookingModel MapToDose2BookingModel(BookingModel existingBooking, BookDose2RequestDto bookDose2Request, int slotNumber, DateTime slotDateTime)
        {
            existingBooking.Dose2HospitalUid = bookDose2Request.Dose2HospitalUid;
            existingBooking.Dose2SlotNumber = slotNumber;
            existingBooking.Dose2RequestedDateTime = slotDateTime;
            existingBooking.IsDose2Completed = false;
            existingBooking.IsD2RequestCanceled = false;   // clear cancellation on rebook
            existingBooking.IsD2RejectedByAdmin = false;   // clear admin-rejection flag on rebook
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

        private async Task<BookingProfileDataDto> MapToBookingProfileDtoAsync(BookingModel booking)
        {
            var (dose1Emails, dose2Emails) = await GetHospitalAdminEmailsAsync(booking.Dose1HospitalUid, booking.Dose2HospitalUid);
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
                IsD1RejectedByAdmin = booking.IsD1RejectedByAdmin,
                IsD2RequestCanceled = booking.IsD2RequestCanceled,
                IsD2RejectedByAdmin = booking.IsD2RejectedByAdmin,
                Dose1DisplayStatus = ComputeDose1DisplayStatus(booking),
                Dose2DisplayStatus = ComputeDose2DisplayStatus(booking),
                VaccinationDisplayStatus = ComputeVaccinationDisplayStatus(booking),
                CreatedAt = booking.CreatedAt,
                ModifiedAt = booking.ModifiedAt,
                Dose1HospitalAdminEmails = dose1Emails,
                Dose2HospitalAdminEmails = dose2Emails
            };
        }

        // Comma-joined email(s) of the active hospital-admin(s) for each dose's hospital — lets
        // the user see who to contact about their appointment. Empty string if none assigned.
        private async Task<(string Dose1Emails, string Dose2Emails)> GetHospitalAdminEmailsAsync(string dose1HospitalUid, string? dose2HospitalUid)
        {
            var dose1Emails = await GetHospitalAdminEmailsForHospitalAsync(dose1HospitalUid);
            var dose2Emails = string.IsNullOrEmpty(dose2HospitalUid)
                ? ""
                : await GetHospitalAdminEmailsForHospitalAsync(dose2HospitalUid);
            return (dose1Emails, dose2Emails);
        }

        private async Task<string> GetHospitalAdminEmailsForHospitalAsync(string hospitalUid)
        {
            if (string.IsNullOrEmpty(hospitalUid)) return "";

            var mappings = await _roleMappingRepository.GetRoleMappingsByRoleTagAsync("hospital-admin", hospitalUid);
            var activeUserUids = mappings.Where(m => m.IsActive).Select(m => m.UserUid).ToList();
            if (activeUserUids.Count == 0) return "";

            var credentialsList = await _userCredentialsRepository.GetCredentialsByUserUidsAsync(activeUserUids);
            return string.Join(", ", credentialsList.Select(c => c.Email));
        }

        private static string ComputeDose1DisplayStatus(BookingModel booking)
        {
            if (booking.IsDose1Completed) return "Completed";
            if (booking.IsD1RequestCanceled && booking.IsD1RejectedByAdmin) return "Rejected";
            if (booking.IsD1RequestCanceled) return "Cancelled";
            return "Pending";
        }

        private static string ComputeDose2DisplayStatus(BookingModel booking)
        {
            if (string.IsNullOrEmpty(booking.Dose2HospitalUid)) return "NotBooked";
            if (booking.IsDose2Completed) return "Completed";
            if (booking.IsD2RequestCanceled && booking.IsD2RejectedByAdmin) return "Rejected";
            if (booking.IsD2RequestCanceled) return "Cancelled";
            return "Pending";
        }

        // The ONLY field user-facing screens (e.g. the profile page) should read for status —
        // restricts display to Vaccinated / PartiallyVaccinated / NotVaccinated / Pending / Rejected,
        // never a raw "Cancelled" for self-cancellations.
        private static string ComputeVaccinationDisplayStatus(BookingModel booking)
        {
            bool dose1SelfCanceled = booking.IsD1RequestCanceled && !booking.IsD1RejectedByAdmin;
            bool dose1Rejected = booking.IsD1RequestCanceled && booking.IsD1RejectedByAdmin && !booking.IsDose1Completed;
            bool dose2Rejected = booking.IsDose1Completed
                && !string.IsNullOrEmpty(booking.Dose2HospitalUid)
                && booking.IsD2RequestCanceled
                && booking.IsD2RejectedByAdmin
                && !booking.IsDose2Completed;

            if (dose1SelfCanceled) return "NotVaccinated";
            if (dose1Rejected) return "Rejected";
            if (booking.IsVaccinationCompleted) return "Vaccinated";
            if (dose2Rejected) return "Rejected";
            if (booking.IsDose1Completed) return "PartiallyVaccinated";
            return "Pending";
        }
    }
}
