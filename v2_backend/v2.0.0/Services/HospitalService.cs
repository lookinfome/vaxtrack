using Vaxtrack.Interfaces;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Dtos.HospitalDtos;
using Vaxtrack.Models;
using Vaxtrack.Interfaces.UtilityInterfaces;


namespace Vaxtrack.Services
{
    public class HospitalService : IHospitalService
    {
        private readonly IHospitalRepository _hospitalRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IUserRoleMappingRepository _roleMappingRepository;
        private readonly IUtilityService _utilityService;
        private readonly ILogger<HospitalService> _logger;

        public HospitalService(IHospitalRepository hospitalRepository, IBookingRepository bookingRepository, IUserRoleMappingRepository roleMappingRepository, IUtilityService utilityService, ILogger<HospitalService> logger)
        {
            _hospitalRepository = hospitalRepository;
            _bookingRepository = bookingRepository;
            _roleMappingRepository = roleMappingRepository;
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
                        callerUserUid, "hospital-admin", foundHospital.HospitalId);

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
                        callerUserUid, "hospital-admin", foundHospital.HospitalId);

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
                        callerUserUid, "hospital-admin", foundHospital.HospitalId);

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

        public async Task DeleteHospitalAsync(string hospitalId)
        {
            /*
             * Delete Logic:
             * -------------
             * Soft-deletes a hospital by setting IsDeleted = true. The record remains
             * in the database but is excluded from all future lookups.
             * Once deleted, any subsequent call with the same hospitalId returns "not found"
             * because the repository filters out soft-deleted records.
             *
             * Edge cases blocked:
             *   - Null hospitalId              → ArgumentNullException thrown before entering try.
             *   - Hospital not found           → throws (includes already-deleted hospitals).
             *   - Active bookings still exist  → throws (admin must cancel/transfer those bookings first).
             *                                    "Active" means a pending dose at this hospital that has
             *                                    not yet been administered or canceled. Blocking here
             *                                    prevents orphaned bookings that reference a non-existent
             *                                    hospital and would cause UpdateAvailableSlotsAsync to fail
             *                                    on any subsequent cancellation or deletion.
             */

            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);

                if (foundHospital is null)
                    throw new Exception($"HospitalService: DeleteHospitalAsync - hospital {hospitalId} not found");

                bool hasActiveBookings = await _bookingRepository.HasActiveBookingsForHospitalAsync(foundHospital.HospitalId);
                if (hasActiveBookings)
                    throw new Exception($"HospitalService: DeleteHospitalAsync - hospital {hospitalId} has active bookings; cancel or transfer them before deleting the hospital");

                foundHospital.IsDeleted = true;
                foundHospital.RemovedDate = DateTime.UtcNow;
                foundHospital.UpdatedDate = DateTime.UtcNow;
                await _hospitalRepository.UpdateHospitalAsync(foundHospital);

                // Cascade: soft-revoke all role mappings scoped to this hospital (e.g. hospital-admin
                // assignments) so they no longer appear in GetUsersInRoleAsync results
                await _roleMappingRepository.RevokeAllMappingsByContextIdAsync(foundHospital.HospitalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalService: DeleteHospitalAsync - {Message}", ex.Message);
                throw new Exception($"HospitalService: DeleteHospitalAsync - {ex.Message}", ex);
            }
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
                HospitalName = hospital.HospitalName,
                HospitalAddress = hospital.HospitalAddress,
                HospitalPinCode = hospital.HospitalPinCode,
                HospitalPhoneNumber = hospital.HospitalPhoneNumber,
                HospitalEmail = hospital.HospitalEmail,
                TotalSlots = hospital.TotalSlots,
                SlotsAvailable = hospital.SlotsAvailable,
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
