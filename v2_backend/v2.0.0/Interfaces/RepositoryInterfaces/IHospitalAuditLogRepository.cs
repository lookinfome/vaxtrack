using Vaxtrack.Models;

namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface IHospitalAuditLogRepository
    {
        Task<HospitalAuditLogModel> CreateEntryAsync(HospitalAuditLogModel entry);
        Task<List<HospitalAuditLogModel>> GetEntriesByHospitalIdAsync(string hospitalId);
    }
}
