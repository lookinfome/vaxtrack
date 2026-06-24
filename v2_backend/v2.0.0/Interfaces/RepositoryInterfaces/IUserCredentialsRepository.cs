using Vaxtrack.Models;

namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface IUserCredentialsRepository
    {
        Task<UserCredentialsModel> CreateCredentialsAsync(UserCredentialsModel credentials);
        Task<UserCredentialsModel?> GetCredentialsByEmailAsync(string email);
        Task<UserCredentialsModel?> GetCredentialsByUserUidAsync(string userUid);
        Task<List<UserCredentialsModel>> GetCredentialsByUserUidsAsync(List<string> userUids);
        Task SoftDeleteCredentialsByUserUidAsync(string userUid);
        Task UpdateEmailAsync(string userUid, string newEmail);
        Task UpdatePasswordHashAsync(string userUid, string newPasswordHash);
    }
}
