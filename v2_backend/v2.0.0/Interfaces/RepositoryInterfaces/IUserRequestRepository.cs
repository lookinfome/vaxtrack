using Vaxtrack.Models;

namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface IUserRequestRepository
    {
        Task<UserRequestModel> CreateAsync(UserRequestModel request);
        Task<UserRequestModel> UpdateAsync(UserRequestModel request);
        Task<UserRequestModel?> GetByIdAsync(int id);
        Task<List<UserRequestModel>> GetPendingByTypeAsync(string requestType);
        Task<List<UserRequestModel>> GetAllPendingAsync();
        Task<List<UserRequestModel>> GetByUserUidAsync(string userUid);
    }
}
