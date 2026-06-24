using Vaxtrack.Dtos.AuthDtos;

namespace Vaxtrack.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto loginRequest);

        // jti + expiresAt extracted from the validated JWT in the controller (no re-parsing needed)
        Task LogoutAsync(string jti, DateTime expiresAt);
    }
}
