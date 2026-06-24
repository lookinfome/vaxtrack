namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface ITokenBlacklistRepository
    {
        Task RevokeTokenAsync(string jti, DateTime expiresAt);
        Task<bool> IsRevokedAsync(string jti);
    }
}
