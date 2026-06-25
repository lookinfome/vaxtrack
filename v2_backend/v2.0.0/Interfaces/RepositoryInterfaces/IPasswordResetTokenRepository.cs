using Vaxtrack.Models;

namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface IPasswordResetTokenRepository
    {
        Task<PasswordResetTokenModel> CreateTokenAsync(string userUid, string token, DateTime expiresAt);

        // Returns the token only if it exists, is not expired, and has not been used
        Task<PasswordResetTokenModel?> GetValidTokenAsync(string token);

        Task MarkTokenAsUsedAsync(int tokenId);
    }
}
