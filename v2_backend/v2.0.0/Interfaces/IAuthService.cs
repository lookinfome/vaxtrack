using Vaxtrack.Dtos.AuthDtos;

namespace Vaxtrack.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto loginRequest);

        // jti + expiresAt extracted from the validated JWT in the controller (no re-parsing needed)
        Task LogoutAsync(string jti, DateTime expiresAt);

        // Forgot-password flow — does not require the user to be logged in
        Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto forgotPasswordRequest);
        Task<ResetForgottenPasswordResponseDto> ResetForgottenPasswordAsync(ResetForgottenPasswordRequestDto resetRequest);

        // Public reactivation-request flow for a disabled account — does not require login
        Task<ForgotPasswordResponseDto> RequestAccountReactivationAsync(RequestAccountReactivationRequestDto request);
    }
}
