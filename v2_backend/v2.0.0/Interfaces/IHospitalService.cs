
using Vaxtrack.Dtos.HospitalDtos;

namespace Vaxtrack.Interfaces
{
    public interface IHospitalService
    {
        Task<CreateHospitalResponseDto> CreateHospitalAsync(CreateHospitalRequestDto createHospitalRequestDto);
        Task<UpdateHospitalResponseDto> UpdateHospitalAsync(UpdateHospitalRequestDto updateHospitalRequest);
        Task<int> UpdateTotalSlotsAsync(string hospitalId, int totalSlots);
        Task<int> UpdateAvailableSlotsAsync(string hospitalId, int availableSlots);
        Task<HospitalProfileDataDto> GetHospitalByIdAsync(string hospitalId);
        Task<List<HospitalProfileDataDto>> GetAllHospitalsAsync();
        Task DeleteHospitalAsync(string hospitalId);
    }
}