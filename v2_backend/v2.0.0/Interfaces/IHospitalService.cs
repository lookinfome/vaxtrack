
using Vaxtrack.Dtos.HospitalDtos;

namespace Vaxtrack.Interfaces
{
    public interface IHospitalService
    {
        Task<CreateHospitalResponseDto> CreateHospitalAsync(CreateHospitalRequestDto createHospitalRequestDto);

        // callerUserUid + callerIsAdmin: platform admin OR hospital-admin scoped to that hospital
        Task<UpdateHospitalResponseDto> UpdateHospitalAsync(UpdateHospitalRequestDto updateHospitalRequest, string callerUserUid, bool callerIsAdmin);
        Task<int> UpdateTotalSlotsAsync(string hospitalId, int totalSlots, string callerUserUid, bool callerIsAdmin);

        // Optional caller params: internal booking-flow calls (slot reservation/release) bypass the
        // auth check by using the defaults (callerIsAdmin = true). Controller calls pass real values.
        Task<int> UpdateAvailableSlotsAsync(string hospitalId, int availableSlots, string callerUserUid = "", bool callerIsAdmin = true);

        Task<HospitalProfileDataDto> GetHospitalByIdAsync(string hospitalId);
        Task<List<HospitalProfileDataDto>> GetAllHospitalsAsync();

        // ── lifecycle ────────────────────────────────────────────────────────────
        // Active -> Disabled: platform admin only, reason required.
        Task<HospitalProfileDataDto> DisableHospitalAsync(string hospitalId, string callerUserUid, bool callerIsAdmin, string comment);

        // Disabled -> PendingReactivation: the hospital's own hospital-admin only.
        Task<HospitalProfileDataDto> RequestReactivationAsync(string hospitalId, string callerUserUid, bool callerIsAdmin, string? comment);

        // PendingReactivation -> Active / Disabled: platform admin only.
        Task<HospitalProfileDataDto> ApproveReactivationAsync(string hospitalId, string callerUserUid, bool callerIsAdmin, string? comment);
        Task<HospitalProfileDataDto> RejectReactivationAsync(string hospitalId, string callerUserUid, bool callerIsAdmin, string? comment);

        // Disabled -> PendingUnregistration: platform admin only, reason required.
        Task<HospitalProfileDataDto> RequestUnregisterAsync(string hospitalId, string callerUserUid, bool callerIsAdmin, string comment);

        // PendingUnregistration -> Disabled: platform admin withdraws its own request,
        // or the hospital-admin declines it — either way nothing is deleted.
        Task<HospitalProfileDataDto> WithdrawUnregisterRequestAsync(string hospitalId, string callerUserUid, bool callerIsAdmin);
        Task<HospitalProfileDataDto> DeclineUnregisterRequestAsync(string hospitalId, string callerUserUid, bool callerIsAdmin, string? comment);

        // PendingUnregistration -> Unregistered (soft-deleted): the hospital's own hospital-admin
        // re-authenticates with their password to confirm. If no hospital-admin is assigned to this
        // hospital, the requesting platform admin may authorize it themselves (no second party exists).
        Task AuthorizeUnregisterAsync(string hospitalId, string callerUserUid, bool callerIsAdmin, string password, string? comment);

        Task<List<HospitalAuditLogDto>> GetHospitalAuditTrailAsync(string hospitalId, string callerUserUid, bool callerIsAdmin);
    }
}