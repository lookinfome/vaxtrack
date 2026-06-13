using Vaxtrack.Models;

namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface IHospitalRepository
    {
        Task<HospitalModel> CreateHospitalAsync(HospitalModel hospitalCreateRequest);
        Task<HospitalModel> UpdateHospitalAsync(HospitalModel hospitalUpdateRequest);
        Task<HospitalModel> UpdateAvailableSlotsAsync(HospitalModel hospitalAvailableSlotsUpdateRequest);
        Task<HospitalModel> UpdateTotalSlotsAsync(HospitalModel hospitalTotalSlotsUpdateRequest);
        Task<HospitalModel?> GetHospitalByIdAsync(string hospitalId);
        Task<List<HospitalModel>?> GetAllHospitalDetailsAsync();
        Task DeleteHospitalAsync(HospitalModel hospitalDeleteRequest);
        Task<bool> IsHospitalExists(string hospitalId);
    }
}