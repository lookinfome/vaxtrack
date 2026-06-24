using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using Vaxtrack.Dtos.AuthDtos;
using Vaxtrack.Interfaces;

namespace Vaxtrack.Controllers
{
    [ApiController]
    [Route("/api/vaxtrack/v1/[controller]/[action]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<LoginResponseDto>> LoginAsync(LoginRequestDto loginRequestDto)
        {
            try
            {
                var loginResponse = await _authService.LoginAsync(loginRequestDto);
                return Ok(loginResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthController: LoginAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> LogoutAsync()
        {
            try
            {
                // Extract jti and expiry directly from the already-validated JWT Principal
                var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? "";
                var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
                var expiresAt = expClaim is not null
                    ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime
                    : DateTime.UtcNow;

                await _authService.LogoutAsync(jti, expiresAt);
                return Ok(new { message = "logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthController: LogoutAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
