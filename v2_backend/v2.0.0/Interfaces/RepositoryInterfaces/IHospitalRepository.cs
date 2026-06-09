using Vaxtrack.Models;

namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface IHospitalRepository
    {
        Task<HospitalModel> AddHospitalAsync(HospitalModel hospitalDetails);
        Task<HospitalModel> UpdateHospitalAsync(HospitalModel hospitalDetails);
        Task<HospitalModel> UpdateAvailableSlotsAsync(string hospitalId, int currentAvailableSlots);
        Task<HospitalModel> GetHospitalByIdAsync(string hospitalId);
        Task<List<HospitalModel>> GetAllHospitalsAsync();
        Task<int> GetSlotsAvailableAsync(string hospitalId);
        Task<int> UpdateTotalSlotsAsync(string hospitalId, int totalSlots);
        Task DeleteHospitalAsync(string hospitalId, DateTime removedDatetime, bool isDeleted);
        Task<bool> IsHospitalExistingAsync(string hospitalId);

    }
}