
using Microsoft.EntityFrameworkCore;
using Vaxtrack.Models;
using Vaxtrack.Interfaces.RepositoryInterfaces;

namespace Vaxtrack.Repositories
{
    public class HospitalRepository: IHospitalRepository
    {
        private readonly VaxtrackDbContext _dbContext;

        public HospitalRepository(VaxtrackDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<HospitalModel> AddHospitalAsync(HospitalModel hospitalDetails)
        {
            _dbContext.Hospitals.Add(hospitalDetails);
            await _dbContext.SaveChangesAsync();
            return hospitalDetails;
        }

        public async Task<HospitalModel> UpdateHospitalAsync(HospitalModel hospitalDetails)
        {
            var foundHospital = await GetHospitalByIdAsync(hospitalDetails.HospitalId);

            // Update the hospital properties
            foundHospital?.HospitalName = hospitalDetails.HospitalName;
            foundHospital?.HospitalAddress = hospitalDetails.HospitalAddress;
            foundHospital?.HospitalPhoneNumber = hospitalDetails.HospitalPhoneNumber;
            foundHospital?.HospitalEmail = hospitalDetails.HospitalEmail;
            foundHospital?.UpdatedDate = hospitalDetails.UpdatedDate;

            _dbContext.Hospitals.Update(foundHospital!);
            await _dbContext.SaveChangesAsync();
            return foundHospital!;
        }

        public async Task<HospitalModel> GetHospitalByIdAsync(string hospitalId)
        {
            var foundHospital = await _dbContext.Hospitals.FindAsync(hospitalId);
            if (foundHospital == null)
            {
                throw new ArgumentException("Hospital not found");
            }

            return foundHospital;
        }

        public async Task<List<HospitalModel>> GetAllHospitalsAsync()
        {
            return await _dbContext.Hospitals.Where(h => !h.IsDeleted).ToListAsync();
        }

        public async Task<int> GetSlotsAvailableAsync(string hospitalId)
        {
            var foundHospital = await GetHospitalByIdAsync(hospitalId);
            return foundHospital.SlotsAvailable;
        }

        public async Task<int> UpdateTotalSlotsAsync(string hospitalId, int totalSlots)
        {
            var foundHospital = await GetHospitalByIdAsync(hospitalId);
            foundHospital.TotalSlots = totalSlots;
            _dbContext.Hospitals.Update(foundHospital);
            await _dbContext.SaveChangesAsync();
            return foundHospital.TotalSlots;
        }

        public async Task<HospitalModel> UpdateAvailableSlotsAsync(string hospitalId, int currentAvailableSlots)
        {
            var foundHospital = await GetHospitalByIdAsync(hospitalId);
            foundHospital.SlotsAvailable = currentAvailableSlots;
            _dbContext.Hospitals.Update(foundHospital);
            await _dbContext.SaveChangesAsync();
            return await GetHospitalByIdAsync(hospitalId);
        }

        public async Task DeleteHospitalAsync(string hospitalId, DateTime removedDatetime, bool isDeleted)
        {
            var foundHospital = await GetHospitalByIdAsync(hospitalId);
            foundHospital.IsDeleted = isDeleted;
            foundHospital.UpdatedDate = removedDatetime;
            foundHospital.RemovedDate = removedDatetime;
            _dbContext.Hospitals.Update(foundHospital);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> IsHospitalExistingAsync(string hospitalId)
        {
            return await _dbContext.Hospitals.AnyAsync(h => h.HospitalId == hospitalId && !h.IsDeleted);
        }
    }
}
