
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

        public async Task<HospitalModel> CreateHospitalAsync(HospitalModel hospitalCreateRequest)
        {
            ArgumentNullException.ThrowIfNull(hospitalCreateRequest);

            _dbContext.Hospitals.Add(hospitalCreateRequest);
            await _dbContext.SaveChangesAsync();
            return hospitalCreateRequest;
        }

        public async Task<HospitalModel> UpdateHospitalAsync(HospitalModel hospitalUpdateRequest)
        {
            ArgumentNullException.ThrowIfNull(hospitalUpdateRequest);

            _dbContext.Hospitals.Update(hospitalUpdateRequest);
            await _dbContext.SaveChangesAsync();
            return hospitalUpdateRequest;
        }

        public async Task<HospitalModel> UpdateAvailableSlotsAsync(HospitalModel hospitalAvailableSlotsUpdateRequest)
        {
            ArgumentNullException.ThrowIfNull(hospitalAvailableSlotsUpdateRequest);

            _dbContext.Hospitals.Update(hospitalAvailableSlotsUpdateRequest);
            await _dbContext.SaveChangesAsync();
            return hospitalAvailableSlotsUpdateRequest;
        }

        public async Task<HospitalModel> UpdateTotalSlotsAsync(HospitalModel hospitalTotalSlotsUpdateRequest)
        {
            ArgumentNullException.ThrowIfNull(hospitalTotalSlotsUpdateRequest);

            _dbContext.Hospitals.Update(hospitalTotalSlotsUpdateRequest);
            await _dbContext.SaveChangesAsync();
            return hospitalTotalSlotsUpdateRequest;
        }

        public async Task<HospitalModel?> GetHospitalByIdAsync(string hospitalId)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            var foundHospital = await _dbContext.Hospitals.Where(h=>h.HospitalId == hospitalId && !h.IsDeleted).FirstOrDefaultAsync();
            return foundHospital;
        }

        public async Task<List<HospitalModel>?> GetAllHospitalDetailsAsync()
        {
            return await _dbContext.Hospitals.Where(u => !u.IsDeleted).ToListAsync();
        }

        public async Task DeleteHospitalAsync(HospitalModel hospitalDeleteRequest)
        {
            ArgumentNullException.ThrowIfNull(hospitalDeleteRequest);

            _dbContext.Hospitals.Update(hospitalDeleteRequest);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> IsHospitalExists(string hospitalId)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            return await _dbContext.Hospitals.AnyAsync(h=>h.HospitalId == hospitalId && !h.IsDeleted);
        }

    }
}
