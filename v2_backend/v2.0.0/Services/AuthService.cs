using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Vaxtrack.Dtos.AuthDtos;
using Vaxtrack.Interfaces;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserCredentialsRepository _credentialsRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITokenBlacklistRepository _tokenBlacklist;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserCredentialsRepository credentialsRepository,
            IUserRepository userRepository,
            ITokenBlacklistRepository tokenBlacklist,
            JwtSettings jwtSettings,
            ILogger<AuthService> logger)
        {
            _credentialsRepository = credentialsRepository;
            _userRepository = userRepository;
            _tokenBlacklist = tokenBlacklist;
            _jwtSettings = jwtSettings;
            _logger = logger;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto loginRequest)
        {
            /*
             * Login Logic:
             * ------------
             * Two-step lookup across separate tables:
             *   1. Find credentials by email → verifies the password hash.
             *   2. Find user profile by UserUid → verifies account is not soft-deleted.
             *
             * Deliberately returns the same error for "credentials not found", "wrong password",
             * and "user deleted" — prevents attackers from discovering which emails are registered
             * (credential enumeration defence).
             *
             * Edge cases blocked:
             *   - Null request       → ArgumentNullException before entering try.
             *   - Email not found    → throws "invalid email or password".
             *   - Wrong password     → BCrypt.Verify false; throws same error.
             *   - User soft-deleted  → GetUserDetailsByUserUidAsync returns null; throws same error.
             */

            ArgumentNullException.ThrowIfNull(loginRequest);

            try
            {
                // Step 1: look up credentials by email and verify password
                var credentials = await _credentialsRepository.GetCredentialsByEmailAsync(loginRequest.Email);

                if (credentials is null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, credentials.PasswordHash))
                    throw new Exception("AuthService: LoginAsync - invalid email or password");

                // Step 2: look up user profile (also confirms account is not soft-deleted)
                var foundUser = await _userRepository.GetUserDetailsByUserUidAsync(credentials.UserUid);

                if (foundUser is null)
                    throw new Exception("AuthService: LoginAsync - invalid email or password");

                var expiry = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);
                var token = GenerateJwtToken(foundUser, credentials.Email, expiry);

                return new LoginResponseDto
                {
                    Token     = token,
                    ExpiresAt = expiry,
                    UserId    = foundUser.UserId,
                    UserName  = foundUser.UserName,
                    Email     = credentials.Email,
                    UserRole  = foundUser.UserRole
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthService: LoginAsync - {Message}", ex.Message);
                throw new Exception($"AuthService: LoginAsync - {ex.Message}", ex);
            }
        }

        private string GenerateJwtToken(UserModel user, string email, DateTime expiry)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   user.UserUid),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Name,  user.UserName),
                new Claim("role", user.UserRole ? "admin" : "user"),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer:             _jwtSettings.Issuer,
                audience:           _jwtSettings.Audience,
                claims:             claims,
                expires:            expiry,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task LogoutAsync(string jti, DateTime expiresAt)
        {
            /*
             * Logout Logic:
             * -------------
             * Adds the token's JTI to the RevokedTokens table.
             * The JWT bearer OnTokenValidated hook (registered in Program.cs) checks this table
             * on every authenticated request — a revoked token is rejected even before its natural expiry.
             *
             * The controller extracts jti and expiresAt from the already-validated JWT Principal,
             * so no re-parsing or signature verification is needed here.
             *
             * The repository opportunistically purges expired rows on each revocation,
             * keeping the blacklist table small without a separate background job.
             *
             * Edge cases blocked:
             *   - Empty jti → throws (no valid token claim to blacklist).
             */

            if (string.IsNullOrEmpty(jti))
                throw new Exception("AuthService: LogoutAsync - token does not contain a valid jti claim");

            try
            {
                await _tokenBlacklist.RevokeTokenAsync(jti, expiresAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthService: LogoutAsync - {Message}", ex.Message);
                throw new Exception($"AuthService: LogoutAsync - {ex.Message}", ex);
            }
        }
    }
}
