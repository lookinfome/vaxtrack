
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
        Task DeleteHospitalAsync(string hospitalId);
    }
}