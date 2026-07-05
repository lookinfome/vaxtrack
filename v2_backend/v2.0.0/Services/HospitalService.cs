using Vaxtrack.Interfaces;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Dtos.HospitalDtos;
using Vaxtrack.Models;
using Vaxtrack.Interfaces.UtilityInterfaces;
using BC = BCrypt.Net.BCrypt;


namespace Vaxtrack.Services
{
    public class HospitalService : IHospitalService
    {
        private readonly IHospitalRepository _hospitalRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IUserRoleMappingRepository _roleMappingRepository;
        private readonly IUserCredentialsRepository _userCredentialsRepository;
        private readonly IHospitalAuditLogRepository _hospitalAuditLogRepository;
        private readonly INotificationService _notificationService;
        private readonly IUtilityService _utilityService;
        private readonly ILogger<HospitalService> _logger;

        public HospitalService(
            IHospitalRepository hospitalRepository,
            IBookingRepository bookingRepository,
            IUserRoleMappingRepository roleMappingRepository,
            IUserCredentialsRepository userCredentialsRepository,
            IHospitalAuditLogRepository hospitalAuditLogRepository,
            INotificationService notificationService,
            IUtilityService utilityService,
            ILogger<HospitalService> logger)
        {
            _hospitalRepository = hospitalRepository;
            _bookingRepository = bookingRepository;
            _roleMappingRepository = roleMappingRepository;
            _userCredentialsRepository = userCredentialsRepository;
            _hospitalAuditLogRepository = hospitalAuditLogRepository;
            _notificationService = notificationService;
            _utilityService = utilityService;
            _logger = logger;
        }

        public async Task<CreateHospitalResponseDto> CreateHospitalAsync(CreateHospitalRequestDto createHospitalRequest)
        {
            /*
             * Create Logic:
             * -------------
             * Registers a new hospital with only the name required at creation.
             * Contact fields (address, phone, email, pin code) start empty and are
             * populated via UpdateHospitalAsync after registration.
             * TotalSlots and SlotsAvailable both default to 50 — admin adjusts
             * total capacity via UpdateTotalSlotsAsync.
             * HospitalId (readable) and HospitalUid (GUID) are system-generated.
             *
             * Edge cases blocked:
             *   - Null request → ArgumentNullException thrown before entering try.
             */

            ArgumentNullException.ThrowIfNull(createHospitalRequest);

            try
            {
                var newHospital = await MapHospitalCreateRequestToHospitalModel(createHospitalRequest);
                var createdHospital = await _hospitalRepository.CreateHospitalAsync(newHospital);
                return MapToCreateHospitalResponseDto(createdHospital);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: CreateHospitalAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: CreateHospitalAsync - {ex.Message}", ex);
            }
        }

        public async Task<UpdateHospitalResponseDto> UpdateHospitalAsync(UpdateHospitalRequestDto updateHospitalRequest, string callerUserUid, bool callerIsAdmin)
        {
            /*
             * Update Logic:
             * -------------
             * Updates a hospital's contact details. The following fields are mutable:
             *   HospitalAddress, HospitalPinCode, HospitalPhoneNumber, HospitalEmail.
             *
             * The following fields are intentionally immutable via this method:
             *   HospitalId (primary key), HospitalUid (system GUID), HospitalName,
             *   TotalSlots (use UpdateTotalSlotsAsync), SlotsAvailable (managed by booking flow),
             *   RegisteredDate.
             *
             * Authorization:
             *   - Platform admin: can update any hospital.
             *   - Hospital-admin: can update only the hospital they manage
             *     (ContextId = HospitalId in UserRoleMappings).
             *
             * Edge cases blocked:
             *   - Null request            → ArgumentNullException thrown before entering try.
             *   - Hospital not found      → throws.
             *   - Caller not authorized   → throws UnauthorizedAccessException.
             */

            ArgumentNullException.ThrowIfNull(updateHospitalRequest);

            try
            {
                string hospitalId = updateHospitalRequest.HospitalId;
                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);

                if (foundHospital is null)
                    throw new Exception($"HospitalService: UpdateHospitalAsync - hospital {hospitalId} not found");

                if (!callerIsAdmin)
                {
                    bool isHospitalAdmin = await _roleMappingRepository.IsUserInRoleAsync(
                        callerUserUid, "hospital-admin", foundHospital.HospitalUid);

                    if (!isHospitalAdmin)
                        throw new UnauthorizedAccessException(
                            $"HospitalService: UpdateHospitalAsync - caller is not authorized to update hospital {hospitalId}");
                }

                var mappedHospital = MapHospitalForUpdate(foundHospital, updateHospitalRequest);
                var updatedHospital = await _hospitalRepository.UpdateHospitalAsync(mappedHospital);
                return MapToUpdateHospitalResponseDto(updatedHospital);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: UpdateHospitalAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: UpdateHospitalAsync - {ex.Message}", ex);
            }
        }

        public async Task<int> UpdateTotalSlotsAsync(string hospitalId, int totalSlots, string callerUserUid, bool callerIsAdmin)
        {
            /*
             * Update Total Slots Logic:
             * -------------------------
             * Sets the maximum vaccination capacity for a hospital.
             * If the new total is less than the current SlotsAvailable, SlotsAvailable is
             * clamped down to match — prevents available count from exceeding total capacity.
             *
             * Authorization:
             *   - Platform admin: can update any hospital's capacity.
             *   - Hospital-admin: can update capacity only for their own hospital.
             *
             * Edge cases blocked:
             *   - Null hospitalId          → ArgumentNullException thrown before entering try.
             *   - Negative totalSlots      → throws (capacity cannot be negative).
             *   - Hospital not found       → throws.
             *   - Caller not authorized    → throws UnauthorizedAccessException.
             *   - newTotal < SlotsAvailable → SlotsAvailable clamped to newTotal automatically.
             */

            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                if (totalSlots < 0)
                    throw new Exception($"HospitalService: UpdateTotalSlotsAsync - total slots cannot be negative");

                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);

                if (foundHospital is null)
                    throw new Exception($"HospitalService: UpdateTotalSlotsAsync - hospital {hospitalId} not found");

                if (!callerIsAdmin)
                {
                    bool isHospitalAdmin = await _roleMappingRepository.IsUserInRoleAsync(
                        callerUserUid, "hospital-admin", foundHospital.HospitalUid);

                    if (!isHospitalAdmin)
                        throw new UnauthorizedAccessException(
                            $"HospitalService: UpdateTotalSlotsAsync - caller is not authorized to update slots for hospital {hospitalId}");
                }

                foundHospital.TotalSlots = totalSlots;

                // If new total is lower than what's currently shown as available, clamp available down
                if (foundHospital.SlotsAvailable > totalSlots)
                    foundHospital.SlotsAvailable = totalSlots;

                foundHospital.UpdatedDate = DateTime.UtcNow;
                var updatedHospital = await _hospitalRepository.UpdateHospitalAsync(foundHospital);
                return updatedHospital.TotalSlots;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: UpdateTotalSlotsAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: UpdateTotalSlotsAsync - {ex.Message}", ex);
            }
        }

        public async Task<int> UpdateAvailableSlotsAsync(string hospitalId, int availableSlots, string callerUserUid = "", bool callerIsAdmin = true)
        {
            /*
             * Update Available Slots Logic:
             * ------------------------------
             * Adjusts the hospital's available slot count by a DELTA, not an absolute value.
             * Callers pass -1 to consume a slot (booking created) and +1 to free a slot
             * (booking canceled or deleted).
             *
             * Authorization:
             *   - Platform admin: can adjust any hospital's available slots.
             *   - Hospital-admin: can adjust slots only for their own hospital.
             *   - Internal calls from BookingService use the default params (callerIsAdmin = true)
             *     and bypass the check — these are system-driven slot adjustments, not user requests.
             *
             * Edge cases blocked:
             *   - Null hospitalId                          → ArgumentNullException thrown before entering try.
             *   - Hospital not found                       → throws.
             *   - Caller not authorized                    → throws UnauthorizedAccessException.
             *   - Result would go below 0 (over-booking)  → throws (slots cannot be negative).
             *   - Result would exceed TotalSlots           → throws (cannot free more slots than capacity allows).
             */

            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);

                if (foundHospital is null)
                    throw new Exception($"HospitalService: UpdateAvailableSlotsAsync - hospital {hospitalId} not found");

                if (!callerIsAdmin)
                {
                    bool isHospitalAdmin = await _roleMappingRepository.IsUserInRoleAsync(
                        callerUserUid, "hospital-admin", foundHospital.HospitalUid);

                    if (!isHospitalAdmin)
                        throw new UnauthorizedAccessException(
                            $"HospitalService: UpdateAvailableSlotsAsync - caller is not authorized to adjust slots for hospital {hospitalId}");
                }

                int newAvailable = foundHospital.SlotsAvailable + availableSlots;

                if (newAvailable < 0)
                    throw new Exception($"HospitalService: UpdateAvailableSlotsAsync - hospital {hospitalId} would have negative available slots");

                if (newAvailable > foundHospital.TotalSlots)
                    throw new Exception($"HospitalService: UpdateAvailableSlotsAsync - hospital {hospitalId} available slots cannot exceed total slots ({foundHospital.TotalSlots})");

                foundHospital.SlotsAvailable = newAvailable;
                foundHospital.UpdatedDate = DateTime.UtcNow;
                var updatedHospital = await _hospitalRepository.UpdateHospitalAsync(foundHospital);
                return updatedHospital.SlotsAvailable;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: UpdateAvailableSlotsAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: UpdateAvailableSlotsAsync - {ex.Message}", ex);
            }
        }

        public async Task<HospitalProfileDataDto> GetHospitalByIdAsync(string hospitalId)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);

                if (foundHospital is null)
                    throw new Exception($"HospitalService: GetHospitalByIdAsync - hospital {hospitalId} not found");

                return MapToHospitalProfileDataDto(foundHospital);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: GetHospitalByIdAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: GetHospitalByIdAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<HospitalProfileDataDto>> GetAllHospitalsAsync()
        {
            try
            {
                var foundHospitalList = await _hospitalRepository.GetAllHospitalDetailsAsync();

                List<HospitalProfileDataDto> hospitalList = [];
                if (foundHospitalList is not null)
                    foreach (var hospital in foundHospitalList)
                        hospitalList.Add(MapToHospitalProfileDataDto(hospital));

                return hospitalList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: GetAllHospitalsAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: GetAllHospitalsAsync - {ex.Message}", ex);
            }
        }

        // ── lifecycle ──────────────────────────────────────────────────────────────

        public async Task<HospitalProfileDataDto> DisableHospitalAsync(string hospitalId, string callerUserUid, bool callerIsAdmin, string comment)
        {
            /*
             * Disable Logic:
             * --------------
             * Platform admin only. Takes a hospital out of service (e.g. under scrutiny) without
             * deleting it — existing bookings there are left untouched, but it drops out of the
             * hospital picker for new bookings. Requires a reason, which is stored on the hospital
             * (StatusComment) and logged to the audit trail.
             *
             * Edge cases blocked:
             *   - Null hospitalId / blank comment → throws before entering try.
             *   - Caller not a platform admin      → throws UnauthorizedAccessException.
             *   - Hospital not found                → throws.
             *   - Hospital not currently Active      → throws (can't disable twice).
             */

            ArgumentNullException.ThrowIfNull(hospitalId);
            ArgumentException.ThrowIfNullOrWhiteSpace(comment);

            try
            {
                if (!callerIsAdmin)
                    throw new UnauthorizedAccessException("HospitalService: DisableHospitalAsync - only a platform admin may disable a hospital");

                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);
                if (foundHospital is null)
                    throw new Exception($"HospitalService: DisableHospitalAsync - hospital {hospitalId} not found");

                if (foundHospital.Status != "Active")
                    throw new Exception($"HospitalService: DisableHospitalAsync - hospital {hospitalId} must be Active to be disabled (current status: {foundHospital.Status})");

                foundHospital.Status = "Disabled";
                foundHospital.StatusComment = comment;
                foundHospital.UpdatedDate = DateTime.UtcNow;
                var updated = await _hospitalRepository.UpdateHospitalAsync(foundHospital);

                await LogAuditAsync(hospitalId, "Disabled", callerUserUid, "admin", comment);
                await NotifyHospitalAdminsAsync(foundHospital.HospitalUid, $"{foundHospital.HospitalName} has been disabled. Reason: {comment}", "/hospital");
                return MapToHospitalProfileDataDto(updated);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: DisableHospitalAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: DisableHospitalAsync - {ex.Message}", ex);
            }
        }

        public async Task<HospitalProfileDataDto> RequestReactivationAsync(string hospitalId, string callerUserUid, bool callerIsAdmin, string? comment)
        {
            /*
             * Request Reactivation Logic:
             * ----------------------------
             * Only the hospital's own hospital-admin may request reactivation of a Disabled
             * hospital. Moves it to PendingReactivation, awaiting a platform admin's decision —
             * it does not reactivate immediately.
             *
             * Edge cases blocked:
             *   - Null hospitalId                        → throws before entering try.
             *   - Caller not the hospital's hospital-admin → throws UnauthorizedAccessException
             *                                                (platform admins do not request on the
             *                                                hospital's behalf — this is a self-service action).
             *   - Hospital not found                      → throws.
             *   - Hospital not currently Disabled          → throws.
             */

            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);
                if (foundHospital is null)
                    throw new Exception($"HospitalService: RequestReactivationAsync - hospital {hospitalId} not found");

                bool isHospitalAdmin = await _roleMappingRepository.IsUserInRoleAsync(callerUserUid, "hospital-admin", foundHospital.HospitalUid);
                if (!isHospitalAdmin)
                    throw new UnauthorizedAccessException($"HospitalService: RequestReactivationAsync - caller is not the hospital-admin for hospital {hospitalId}");

                if (foundHospital.Status != "Disabled")
                    throw new Exception($"HospitalService: RequestReactivationAsync - hospital {hospitalId} must be Disabled to request reactivation (current status: {foundHospital.Status})");

                foundHospital.Status = "PendingReactivation";
                foundHospital.StatusComment = comment;
                foundHospital.UpdatedDate = DateTime.UtcNow;
                var updated = await _hospitalRepository.UpdateHospitalAsync(foundHospital);

                await LogAuditAsync(hospitalId, "ReactivationRequested", callerUserUid, "hospital-admin", comment);
                await _notificationService.NotifyAllAdminsAsync($"Reactivation requested for {foundHospital.HospitalName}.", "/hospital");
                return MapToHospitalProfileDataDto(updated);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: RequestReactivationAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: RequestReactivationAsync - {ex.Message}", ex);
            }
        }

        public async Task<HospitalProfileDataDto> ApproveReactivationAsync(string hospitalId, string callerUserUid, bool callerIsAdmin, string? comment)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                if (!callerIsAdmin)
                    throw new UnauthorizedAccessException("HospitalService: ApproveReactivationAsync - only a platform admin may approve reactivation");

                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);
                if (foundHospital is null)
                    throw new Exception($"HospitalService: ApproveReactivationAsync - hospital {hospitalId} not found");

                if (foundHospital.Status != "PendingReactivation")
                    throw new Exception($"HospitalService: ApproveReactivationAsync - hospital {hospitalId} has no pending reactivation request (current status: {foundHospital.Status})");

                foundHospital.Status = "Active";
                foundHospital.StatusComment = comment;
                foundHospital.UpdatedDate = DateTime.UtcNow;
                var updated = await _hospitalRepository.UpdateHospitalAsync(foundHospital);

                await LogAuditAsync(hospitalId, "ReactivationApproved", callerUserUid, "admin", comment);
                await NotifyHospitalAdminsAsync(foundHospital.HospitalUid, $"{foundHospital.HospitalName} has been reactivated.", "/hospital");
                return MapToHospitalProfileDataDto(updated);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: ApproveReactivationAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: ApproveReactivationAsync - {ex.Message}", ex);
            }
        }

        public async Task<HospitalProfileDataDto> RejectReactivationAsync(string hospitalId, string callerUserUid, bool callerIsAdmin, string? comment)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                if (!callerIsAdmin)
                    throw new UnauthorizedAccessException("HospitalService: RejectReactivationAsync - only a platform admin may reject reactivation");

                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);
                if (foundHospital is null)
                    throw new Exception($"HospitalService: RejectReactivationAsync - hospital {hospitalId} not found");

                if (foundHospital.Status != "PendingReactivation")
                    throw new Exception($"HospitalService: RejectReactivationAsync - hospital {hospitalId} has no pending reactivation request (current status: {foundHospital.Status})");

                foundHospital.Status = "Disabled";
                foundHospital.StatusComment = comment;
                foundHospital.UpdatedDate = DateTime.UtcNow;
                var updated = await _hospitalRepository.UpdateHospitalAsync(foundHospital);

                await LogAuditAsync(hospitalId, "ReactivationRejected", callerUserUid, "admin", comment);
                await NotifyHospitalAdminsAsync(foundHospital.HospitalUid, $"Reactivation request for {foundHospital.HospitalName} was rejected.{(string.IsNullOrWhiteSpace(comment) ? "" : $" Reason: {comment}")}", "/hospital");
                return MapToHospitalProfileDataDto(updated);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: RejectReactivationAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: RejectReactivationAsync - {ex.Message}", ex);
            }
        }

        public async Task<HospitalProfileDataDto> RequestUnregisterAsync(string hospitalId, string callerUserUid, bool callerIsAdmin, string comment)
        {
            /*
             * Request Unregister Logic:
             * --------------------------
             * Platform admin only, and only once a hospital is already Disabled (forces a
             * two-step wind-down: disable with a reason, then unregister with a reason —
             * mirrors the 30-day-scrutiny-then-closure scenario). Moves the hospital to
             * PendingUnregistration; nothing is deleted yet until AuthorizeUnregisterAsync runs.
             */

            ArgumentNullException.ThrowIfNull(hospitalId);
            ArgumentException.ThrowIfNullOrWhiteSpace(comment);

            try
            {
                if (!callerIsAdmin)
                    throw new UnauthorizedAccessException("HospitalService: RequestUnregisterAsync - only a platform admin may request unregistration");

                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);
                if (foundHospital is null)
                    throw new Exception($"HospitalService: RequestUnregisterAsync - hospital {hospitalId} not found");

                if (foundHospital.Status != "Disabled")
                    throw new Exception($"HospitalService: RequestUnregisterAsync - hospital {hospitalId} must be Disabled before it can be unregistered (current status: {foundHospital.Status})");

                foundHospital.Status = "PendingUnregistration";
                foundHospital.StatusComment = comment;
                foundHospital.UpdatedDate = DateTime.UtcNow;
                var updated = await _hospitalRepository.UpdateHospitalAsync(foundHospital);

                await LogAuditAsync(hospitalId, "UnregisterRequested", callerUserUid, "admin", comment);
                await NotifyHospitalAdminsAsync(foundHospital.HospitalUid, $"Unregistration requested for {foundHospital.HospitalName}. Reason: {comment}", "/hospital");
                return MapToHospitalProfileDataDto(updated);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: RequestUnregisterAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: RequestUnregisterAsync - {ex.Message}", ex);
            }
        }

        public async Task<HospitalProfileDataDto> WithdrawUnregisterRequestAsync(string hospitalId, string callerUserUid, bool callerIsAdmin)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                if (!callerIsAdmin)
                    throw new UnauthorizedAccessException("HospitalService: WithdrawUnregisterRequestAsync - only a platform admin may withdraw an unregister request");

                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);
                if (foundHospital is null)
                    throw new Exception($"HospitalService: WithdrawUnregisterRequestAsync - hospital {hospitalId} not found");

                if (foundHospital.Status != "PendingUnregistration")
                    throw new Exception($"HospitalService: WithdrawUnregisterRequestAsync - hospital {hospitalId} has no pending unregister request (current status: {foundHospital.Status})");

                foundHospital.Status = "Disabled";
                foundHospital.UpdatedDate = DateTime.UtcNow;
                var updated = await _hospitalRepository.UpdateHospitalAsync(foundHospital);

                await LogAuditAsync(hospitalId, "UnregisterWithdrawn", callerUserUid, "admin", null);
                await NotifyHospitalAdminsAsync(foundHospital.HospitalUid, $"The unregistration request for {foundHospital.HospitalName} was withdrawn.", "/hospital");
                return MapToHospitalProfileDataDto(updated);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: WithdrawUnregisterRequestAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: WithdrawUnregisterRequestAsync - {ex.Message}", ex);
            }
        }

        public async Task<HospitalProfileDataDto> DeclineUnregisterRequestAsync(string hospitalId, string callerUserUid, bool callerIsAdmin, string? comment)
        {
            /*
             * The hospital-admin's side of "changing their mind" about an unregister request —
             * symmetric to WithdrawUnregisterRequestAsync (the platform admin's side). Either
             * path returns the hospital to Disabled without deleting anything.
             */

            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);
                if (foundHospital is null)
                    throw new Exception($"HospitalService: DeclineUnregisterRequestAsync - hospital {hospitalId} not found");

                bool isHospitalAdmin = await _roleMappingRepository.IsUserInRoleAsync(callerUserUid, "hospital-admin", foundHospital.HospitalUid);
                if (!isHospitalAdmin)
                    throw new UnauthorizedAccessException($"HospitalService: DeclineUnregisterRequestAsync - caller is not the hospital-admin for hospital {hospitalId}");

                if (foundHospital.Status != "PendingUnregistration")
                    throw new Exception($"HospitalService: DeclineUnregisterRequestAsync - hospital {hospitalId} has no pending unregister request (current status: {foundHospital.Status})");

                foundHospital.Status = "Disabled";
                foundHospital.UpdatedDate = DateTime.UtcNow;
                var updated = await _hospitalRepository.UpdateHospitalAsync(foundHospital);

                await LogAuditAsync(hospitalId, "UnregisterDeclined", callerUserUid, "hospital-admin", comment);
                await _notificationService.NotifyAllAdminsAsync($"The hospital-admin declined the unregistration request for {foundHospital.HospitalName}.", "/hospital");
                return MapToHospitalProfileDataDto(updated);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: DeclineUnregisterRequestAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: DeclineUnregisterRequestAsync - {ex.Message}", ex);
            }
        }

        public async Task AuthorizeUnregisterAsync(string hospitalId, string callerUserUid, bool callerIsAdmin, string password, string? comment)
        {
            /*
             * Authorize Unregister Logic:
             * -----------------------------
             * The second-party check on a platform admin's unregister request: the hospital's own
             * hospital-admin re-enters their login password to confirm, then the hospital is
             * soft-deleted (same mechanism as the old direct-delete: IsDeleted = true).
             *
             * Fallback: if no hospital-admin is currently assigned to this hospital, the requesting
             * platform admin may authorize it themselves (with their own password) — there is no
             * second party to check against, so the platform admin's authority is sufficient.
             *
             * Edge cases blocked:
             *   - Hospital not found                  → throws.
             *   - Hospital not PendingUnregistration    → throws.
             *   - Caller not the hospital-admin (and no fallback applies) → throws UnauthorizedAccessException.
             *   - Wrong password                       → throws ArgumentException (friendly 400, not 403 —
             *                                             this is a credential mismatch, not a role failure).
             *   - Active bookings still exist           → throws (must be resolved first).
             */

            ArgumentNullException.ThrowIfNull(hospitalId);
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            try
            {
                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);
                if (foundHospital is null)
                    throw new Exception($"HospitalService: AuthorizeUnregisterAsync - hospital {hospitalId} not found");

                if (foundHospital.Status != "PendingUnregistration")
                    throw new Exception($"HospitalService: AuthorizeUnregisterAsync - hospital {hospitalId} has no pending unregister request (current status: {foundHospital.Status})");

                bool isHospitalAdmin = await _roleMappingRepository.IsUserInRoleAsync(callerUserUid, "hospital-admin", foundHospital.HospitalUid);

                if (!isHospitalAdmin)
                {
                    var assignedAdmins = await _roleMappingRepository.GetRoleMappingsByRoleTagAsync("hospital-admin", foundHospital.HospitalUid);
                    bool anyHospitalAdminAssigned = assignedAdmins.Any(m => m.IsActive);

                    if (!callerIsAdmin || anyHospitalAdminAssigned)
                        throw new UnauthorizedAccessException($"HospitalService: AuthorizeUnregisterAsync - caller is not the hospital-admin for hospital {hospitalId}");
                }

                var credentials = await _userCredentialsRepository.GetCredentialsByUserUidAsync(callerUserUid);
                if (credentials is null || !BC.Verify(password, credentials.PasswordHash))
                    throw new ArgumentException("Incorrect password. Please try again.");

                bool hasActiveBookings = await _bookingRepository.HasActiveBookingsForHospitalAsync(foundHospital.HospitalUid);
                if (hasActiveBookings)
                    throw new Exception($"HospitalService: AuthorizeUnregisterAsync - hospital {hospitalId} has active bookings; cancel or transfer them before unregistering");

                foundHospital.Status = "Unregistered";
                if (!string.IsNullOrWhiteSpace(comment)) foundHospital.StatusComment = comment;
                foundHospital.IsDeleted = true;
                foundHospital.RemovedDate = DateTime.UtcNow;
                foundHospital.UpdatedDate = DateTime.UtcNow;
                await _hospitalRepository.UpdateHospitalAsync(foundHospital);

                // Cascade: soft-revoke all role mappings scoped to this hospital (e.g. hospital-admin
                // assignments) so they no longer appear in GetUsersInRoleAsync results
                await _roleMappingRepository.RevokeAllMappingsByContextIdAsync(foundHospital.HospitalUid);

                await LogAuditAsync(hospitalId, "Unregistered", callerUserUid, isHospitalAdmin ? "hospital-admin" : "admin", comment);
                await _notificationService.NotifyAllAdminsAsync($"{foundHospital.HospitalName} has been unregistered.", "/hospital");
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (ArgumentException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: AuthorizeUnregisterAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: AuthorizeUnregisterAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<HospitalAuditLogDto>> GetHospitalAuditTrailAsync(string hospitalId, string callerUserUid, bool callerIsAdmin)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                // Includes soft-deleted (Unregistered) hospitals — the audit trail must remain
                // readable after a hospital is unregistered, not just while it's live.
                var foundHospital = await _hospitalRepository.GetHospitalByIdIncludingDeletedAsync(hospitalId);
                if (foundHospital is null)
                    throw new Exception($"HospitalService: GetHospitalAuditTrailAsync - hospital {hospitalId} not found");

                if (!callerIsAdmin)
                {
                    bool isHospitalAdmin = await _roleMappingRepository.IsUserInRoleAsync(callerUserUid, "hospital-admin", foundHospital.HospitalUid);
                    if (!isHospitalAdmin)
                        throw new UnauthorizedAccessException($"HospitalService: GetHospitalAuditTrailAsync - caller is not authorized to view the audit trail for hospital {hospitalId}");
                }

                var entries = await _hospitalAuditLogRepository.GetEntriesByHospitalIdAsync(hospitalId);
                return entries.Select(MapToHospitalAuditLogDto).ToList();
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: GetHospitalAuditTrailAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: GetHospitalAuditTrailAsync - {ex.Message}", ex);
            }
        }

        private async Task LogAuditAsync(string hospitalId, string actionType, string actorUserUid, string actorRole, string? comment)
        {
            await _hospitalAuditLogRepository.CreateEntryAsync(new HospitalAuditLogModel
            {
                HospitalId = hospitalId,
                ActionType = actionType,
                ActorUserUid = actorUserUid,
                ActorRole = actorRole,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Notifies every active hospital-admin assigned to this hospital (there may be more than one).
        private async Task NotifyHospitalAdminsAsync(string hospitalUid, string message, string? linkPath = null)
        {
            var mappings = await _roleMappingRepository.GetRoleMappingsByRoleTagAsync("hospital-admin", hospitalUid);
            foreach (var mapping in mappings.Where(m => m.IsActive))
                await _notificationService.NotifyAsync(mapping.UserUid, message, linkPath);
        }

        private static HospitalAuditLogDto MapToHospitalAuditLogDto(HospitalAuditLogModel entry)
        {
            return new HospitalAuditLogDto
            {
                HospitalId = entry.HospitalId,
                ActionType = entry.ActionType,
                ActorUserUid = entry.ActorUserUid,
                ActorRole = entry.ActorRole,
                Comment = entry.Comment,
                CreatedAt = entry.CreatedAt
            };
        }

        // ── private mapping helpers ───────────────────────────────────────────────

        private async Task<HospitalModel> MapHospitalCreateRequestToHospitalModel(CreateHospitalRequestDto createHospitalRequest)
        {
            var timestamp = DateTime.UtcNow;
            var guid = await _utilityService.GenerateGuidAsync();
            var uniqueId = await _utilityService.GenerateUniqueIdAsync(createHospitalRequest.HospitalName);

            return new HospitalModel
            {
                HospitalId = uniqueId,
                HospitalUid = guid,
                HospitalName = createHospitalRequest.HospitalName,
                HospitalAddress = "",
                HospitalPhoneNumber = "",
                HospitalPinCode = "",
                HospitalEmail = "",
                TotalSlots = 50,
                SlotsAvailable = 50,
                RegisteredDate = timestamp,
                UpdatedDate = timestamp
            };
        }

        private static HospitalModel MapHospitalForUpdate(HospitalModel foundHospital, UpdateHospitalRequestDto updateHospitalRequest)
        {
            foundHospital.HospitalAddress = updateHospitalRequest.HospitalAddress;
            foundHospital.HospitalPinCode = updateHospitalRequest.HospitalPinCode;
            foundHospital.HospitalPhoneNumber = updateHospitalRequest.HospitalPhoneNumber;
            foundHospital.HospitalEmail = updateHospitalRequest.HospitalEmail;
            foundHospital.UpdatedDate = DateTime.UtcNow;
            return foundHospital;
        }

        private static CreateHospitalResponseDto MapToCreateHospitalResponseDto(HospitalModel hospital)
        {
            return new CreateHospitalResponseDto
            {
                HospitalId = hospital.HospitalId,
                HospitalName = hospital.HospitalName,
                HospitalAddress = hospital.HospitalAddress,
                HospitalPinCode = hospital.HospitalPinCode,
                HospitalPhoneNumber = hospital.HospitalPhoneNumber,
                HospitalEmail = hospital.HospitalEmail,
                TotalSlots = hospital.TotalSlots,
                SlotsAvailable = hospital.SlotsAvailable,
                RegisteredDate = hospital.RegisteredDate
            };
        }

        private static HospitalProfileDataDto MapToHospitalProfileDataDto(HospitalModel hospital)
        {
            return new HospitalProfileDataDto
            {
                HospitalId = hospital.HospitalId,
                HospitalUid = hospital.HospitalUid,
                HospitalName = hospital.HospitalName,
                HospitalAddress = hospital.HospitalAddress,
                HospitalPinCode = hospital.HospitalPinCode,
                HospitalPhoneNumber = hospital.HospitalPhoneNumber,
                HospitalEmail = hospital.HospitalEmail,
                TotalSlots = hospital.TotalSlots,
                SlotsAvailable = hospital.SlotsAvailable,
                Status = hospital.Status,
                StatusComment = hospital.StatusComment,
                RegisteredDate = hospital.RegisteredDate,
                UpdatedDate = hospital.UpdatedDate
            };
        }

        private static UpdateHospitalResponseDto MapToUpdateHospitalResponseDto(HospitalModel hospital)
        {
            return new UpdateHospitalResponseDto
            {
                HospitalId = hospital.HospitalId,
                HospitalAddress = hospital.HospitalAddress,
                HospitalPinCode = hospital.HospitalPinCode,
                HospitalPhoneNumber = hospital.HospitalPhoneNumber,
                HospitalEmail = hospital.HospitalEmail
            };
        }
    }
}
