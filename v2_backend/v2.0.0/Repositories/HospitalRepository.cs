using Microsoft.EntityFrameworkCore;
using Vaxtrack.Models;
using Vaxtrack.Interfaces.RepositoryInterfaces;

namespace Vaxtrack.Repositories
{
    public class HospitalRepository : IHospitalRepository
    {
        private readonly VaxtrackDbContext _dbContext;
        private readonly ILogger<HospitalRepository> _logger;

        public HospitalRepository(VaxtrackDbContext dbContext, ILogger<HospitalRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<HospitalModel> CreateHospitalAsync(HospitalModel hospitalCreateRequest)
        {
            ArgumentNullException.ThrowIfNull(hospitalCreateRequest);

            try
            {
                _dbContext.Hospitals.Add(hospitalCreateRequest);
                await _dbContext.SaveChangesAsync();
                return hospitalCreateRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalRepository: CreateHospitalAsync - {Message}", ex.Message);
                throw new Exception($"HospitalRepository: CreateHospitalAsync - {ex.Message}", ex);
            }
        }

        public async Task<HospitalModel> UpdateHospitalAsync(HospitalModel hospitalUpdateRequest)
        {
            ArgumentNullException.ThrowIfNull(hospitalUpdateRequest);

            try
            {
                _dbContext.Hospitals.Update(hospitalUpdateRequest);
                await _dbContext.SaveChangesAsync();
                return hospitalUpdateRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalRepository: UpdateHospitalAsync - {Message}", ex.Message);
                throw new Exception($"HospitalRepository: UpdateHospitalAsync - {ex.Message}", ex);
            }
        }

        public async Task<HospitalModel> UpdateAvailableSlotsAsync(HospitalModel hospitalAvailableSlotsUpdateRequest)
        {
            ArgumentNullException.ThrowIfNull(hospitalAvailableSlotsUpdateRequest);

            try
            {
                _dbContext.Hospitals.Update(hospitalAvailableSlotsUpdateRequest);
                await _dbContext.SaveChangesAsync();
                return hospitalAvailableSlotsUpdateRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalRepository: UpdateAvailableSlotsAsync - {Message}", ex.Message);
                throw new Exception($"HospitalRepository: UpdateAvailableSlotsAsync - {ex.Message}", ex);
            }
        }

        public async Task<HospitalModel> UpdateTotalSlotsAsync(HospitalModel hospitalTotalSlotsUpdateRequest)
        {
            ArgumentNullException.ThrowIfNull(hospitalTotalSlotsUpdateRequest);

            try
            {
                _dbContext.Hospitals.Update(hospitalTotalSlotsUpdateRequest);
                await _dbContext.SaveChangesAsync();
                return hospitalTotalSlotsUpdateRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalRepository: UpdateTotalSlotsAsync - {Message}", ex.Message);
                throw new Exception($"HospitalRepository: UpdateTotalSlotsAsync - {ex.Message}", ex);
            }
        }

        public async Task<HospitalModel?> GetHospitalByIdAsync(string hospitalId)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                return await _dbContext.Hospitals.Where(h => h.HospitalId == hospitalId && !h.IsDeleted).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalRepository: GetHospitalByIdAsync - {Message}", ex.Message);
                throw new Exception($"HospitalRepository: GetHospitalByIdAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<HospitalModel>?> GetAllHospitalDetailsAsync()
        {
            try
            {
                return await _dbContext.Hospitals.Where(h => !h.IsDeleted).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalRepository: GetAllHospitalDetailsAsync - {Message}", ex.Message);
                throw new Exception($"HospitalRepository: GetAllHospitalDetailsAsync - {ex.Message}", ex);
            }
        }

        public async Task DeleteHospitalAsync(HospitalModel hospitalDeleteRequest)
        {
            ArgumentNullException.ThrowIfNull(hospitalDeleteRequest);

            try
            {
                _dbContext.Hospitals.Update(hospitalDeleteRequest);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalRepository: DeleteHospitalAsync - {Message}", ex.Message);
                throw new Exception($"HospitalRepository: DeleteHospitalAsync - {ex.Message}", ex);
            }
        }

        public async Task<bool> IsHospitalExists(string hospitalId)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                return await _dbContext.Hospitals.AnyAsync(h => h.HospitalId == hospitalId && !h.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalRepository: IsHospitalExists - {Message}", ex.Message);
                throw new Exception($"HospitalRepository: IsHospitalExists - {ex.Message}", ex);
            }
        }
    }
}
