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

        // Includes soft-deleted (Unregistered) hospitals — used only for reading historical
        // audit trail entries, which must remain viewable after a hospital is unregistered.
        Task<HospitalModel?> GetHospitalByIdIncludingDeletedAsync(string hospitalId);
        Task<List<HospitalModel>?> GetAllHospitalDetailsAsync();
        Task DeleteHospitalAsync(HospitalModel hospitalDeleteRequest);
        Task<bool> IsHospitalExists(string hospitalId);
    }
}