using Microsoft.EntityFrameworkCore;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Repositories
{
    public class HospitalAuditLogRepository : IHospitalAuditLogRepository
    {
        private readonly VaxtrackDbContext _dbContext;
        private readonly ILogger<HospitalAuditLogRepository> _logger;

        public HospitalAuditLogRepository(VaxtrackDbContext dbContext, ILogger<HospitalAuditLogRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<HospitalAuditLogModel> CreateEntryAsync(HospitalAuditLogModel entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            try
            {
                _dbContext.HospitalAuditLogs.Add(entry);
                await _dbContext.SaveChangesAsync();
                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalAuditLogRepository: CreateEntryAsync - {Message}", ex.Message);
                throw new Exception($"HospitalAuditLogRepository: CreateEntryAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<HospitalAuditLogModel>> GetEntriesByHospitalIdAsync(string hospitalId)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                return await _dbContext.HospitalAuditLogs
                    .Where(e => e.HospitalId == hospitalId)
                    .OrderBy(e => e.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalAuditLogRepository: GetEntriesByHospitalIdAsync - {Message}", ex.Message);
                throw new Exception($"HospitalAuditLogRepository: GetEntriesByHospitalIdAsync - {ex.Message}", ex);
            }
        }
    }
}
