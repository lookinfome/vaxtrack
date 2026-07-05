using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Vaxtrack.Dtos.AuthDtos;
using Vaxtrack.Exceptions;
using Vaxtrack.Interfaces;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;
using BC = BCrypt.Net.BCrypt;

namespace Vaxtrack.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserCredentialsRepository _credentialsRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserService _userService;
        private readonly IUserRequestRepository _userRequestRepository;
        private readonly INotificationService _notificationService;
        private readonly ITokenBlacklistRepository _tokenBlacklist;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly IEmailService _emailService;
        private readonly JwtSettings _jwtSettings;
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserCredentialsRepository credentialsRepository,
            IUserRepository userRepository,
            IUserService userService,
            IUserRequestRepository userRequestRepository,
            INotificationService notificationService,
            ITokenBlacklistRepository tokenBlacklist,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            IEmailService emailService,
            JwtSettings jwtSettings,
            SmtpSettings smtpSettings,
            ILogger<AuthService> logger)
        {
            _credentialsRepository = credentialsRepository;
            _userRepository = userRepository;
            _userService = userService;
            _userRequestRepository = userRequestRepository;
            _notificationService = notificationService;
            _tokenBlacklist = tokenBlacklist;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _emailService = emailService;
            _jwtSettings = jwtSettings;
            _smtpSettings = smtpSettings;
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

                if (credentials is null || !BC.Verify(loginRequest.Password, credentials.PasswordHash))
                    throw new Exception("AuthService: LoginAsync - invalid email or password");

                // Step 2: look up user profile (also confirms account is not soft-deleted)
                var foundUser = await _userRepository.GetUserDetailsByUserUidAsync(credentials.UserUid);

                if (foundUser is null)
                    throw new Exception("AuthService: LoginAsync - invalid email or password");

                // Checked only after password verification succeeds — never before — so a wrong
                // password guess can't be used to discover whether an account is disabled.
                if (foundUser.Status == "Disabled" || foundUser.Status == "PendingReactivation")
                    throw new AccountDisabledException(foundUser.StatusComment);

                var expiry = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);
                var token = GenerateJwtToken(foundUser, credentials.Email, expiry);

                return new LoginResponseDto
                {
                    Token     = token,
                    ExpiresAt = expiry,
                    UserId    = foundUser.UserId,
                    UserUid   = foundUser.UserUid,
                    UserName  = foundUser.UserName,
                    Email     = credentials.Email,
                    UserRole  = foundUser.UserRole
                };
            }
            catch (AccountDisabledException)
            {
                throw;
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

        public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto forgotPasswordRequest)
        {
            /*
             * Forgot Password Logic:
             * ----------------------
             * Generates a time-limited, single-use reset token for an account identified by email.
             * The caller is NOT logged in — this is the "I forgot my password" path.
             *
             * Security: the response is always the same generic message whether the email is
             * registered or not. This prevents email enumeration (an attacker cannot determine
             * which emails exist on the platform from this endpoint).
             *
             * Token delivery: the reset link is emailed via IEmailService and never included
             * in the response body. If the email fails to send, the failure is logged but the
             * request still returns the generic success message (does not leak account existence).
             *
             * Edge cases blocked:
             *   - Null request       → ArgumentNullException.
             *   - Email not found    → returns generic response, no token generated.
             *   - User soft-deleted  → GetUserDetailsByUserUidAsync returns null → same generic response.
             */

            ArgumentNullException.ThrowIfNull(forgotPasswordRequest);

            const string genericMessage = "If this email is registered, a password reset token has been sent.";

            try
            {
                var credentials = await _credentialsRepository.GetCredentialsByEmailAsync(forgotPasswordRequest.Email);

                if (credentials is null)
                    return new ForgotPasswordResponseDto { Message = genericMessage };

                var foundUser = await _userRepository.GetUserDetailsByUserUidAsync(credentials.UserUid);

                if (foundUser is null)
                    return new ForgotPasswordResponseDto { Message = genericMessage };

                var token     = Guid.NewGuid().ToString("N");   // 32 hex chars, no dashes
                var expiresAt = DateTime.UtcNow.AddMinutes(15);

                await _passwordResetTokenRepository.CreateTokenAsync(foundUser.UserUid, token, expiresAt);

                var resetLink = $"{_smtpSettings.FrontendResetPasswordUrl}?token={token}";
                try
                {
                    await _emailService.SendPasswordResetEmailAsync(credentials.Email, resetLink);
                }
                catch (Exception ex)
                {
                    // Log but do not fail the request or leak whether the email exists/send succeeded —
                    // the generic response goes out either way (enumeration defence, see method header).
                    _logger.LogError(ex, "AuthService: ForgotPasswordAsync - failed to send reset email for {Email}", forgotPasswordRequest.Email);
                }

                return new ForgotPasswordResponseDto { Message = genericMessage };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthService: ForgotPasswordAsync - {Message}", ex.Message);
                throw new Exception($"AuthService: ForgotPasswordAsync - {ex.Message}", ex);
            }
        }

        public async Task<ForgotPasswordResponseDto> RequestAccountReactivationAsync(RequestAccountReactivationRequestDto request)
        {
            /*
             * Public Reactivation Request Logic:
             * ------------------------------------
             * The caller is NOT logged in — a disabled account can never authenticate (see
             * LoginAsync), so this is the only path back for them. Identified by email rather
             * than a JWT.
             *
             * Security: always returns the same generic message regardless of whether the email
             * matches a real, disabled account — same anti-enumeration convention as ForgotPasswordAsync.
             * Silently no-ops (via UserService.MarkPendingReactivationAsync) if the account isn't
             * actually Disabled.
             */

            ArgumentNullException.ThrowIfNull(request);

            const string genericMessage = "If this account is disabled, your reactivation request has been submitted for review.";

            try
            {
                var credentials = await _credentialsRepository.GetCredentialsByEmailAsync(request.Email);
                if (credentials is null)
                    return new ForgotPasswordResponseDto { Message = genericMessage };

                var foundUser = await _userRepository.GetUserDetailsByUserUidAsync(credentials.UserUid);
                if (foundUser is null || foundUser.Status != "Disabled")
                    return new ForgotPasswordResponseDto { Message = genericMessage };

                await _userService.MarkPendingReactivationAsync(foundUser.UserUid);

                await _userRequestRepository.CreateAsync(new UserRequestModel
                {
                    UserUid = foundUser.UserUid,
                    RequestType = "AccountReactivation",
                    Status = "Pending",
                    UserComment = request.Reason,
                    CreatedAt = DateTime.UtcNow
                });

                await _notificationService.NotifyAllAdminsAsync(
                    $"{foundUser.UserName} has requested account reactivation.", "/admin/users");

                return new ForgotPasswordResponseDto { Message = genericMessage };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthService: RequestAccountReactivationAsync - {Message}", ex.Message);
                throw new Exception($"AuthService: RequestAccountReactivationAsync - {ex.Message}", ex);
            }
        }

        public async Task<ResetForgottenPasswordResponseDto> ResetForgottenPasswordAsync(ResetForgottenPasswordRequestDto resetRequest)
        {
            /*
             * Reset Forgotten Password Logic:
             * --------------------------------
             * Validates the reset token (must exist, not expired, not already used), then
             * hashes the new password and updates UserCredentials.
             * The token is immediately marked as used so it cannot be replayed.
             *
             * This is different from ChangePasswordAsync:
             *   - ChangePasswordAsync: user is logged in and supplies their current password.
             *   - ResetForgottenPasswordAsync: user is NOT logged in; identity is proved by
             *     possessing a valid, time-limited token that was issued to their email.
             *
             * Edge cases blocked:
             *   - Null request              → ArgumentNullException.
             *   - Token not found           → throws (invalid or never issued).
             *   - Token expired             → throws (GetValidTokenAsync returns null for expired rows).
             *   - Token already used        → throws (IsUsed = true filtered out by repository).
             *   - User or credentials gone  → throws (account may have been deleted since token was issued).
             */

            ArgumentNullException.ThrowIfNull(resetRequest);

            try
            {
                var resetToken = await _passwordResetTokenRepository.GetValidTokenAsync(resetRequest.ResetToken);

                if (resetToken is null)
                    throw new Exception("AuthService: ResetForgottenPasswordAsync - reset token is invalid or has expired");

                var credentials = await _credentialsRepository.GetCredentialsByUserUidAsync(resetToken.UserUid);

                if (credentials is null)
                    throw new Exception("AuthService: ResetForgottenPasswordAsync - no active credentials found for this token");

                var foundUser = await _userRepository.GetUserDetailsByUserUidAsync(resetToken.UserUid);

                if (foundUser is null)
                    throw new Exception("AuthService: ResetForgottenPasswordAsync - user account not found or has been deleted");

                // Mark the token used before updating the password — prevents any race-condition replay
                await _passwordResetTokenRepository.MarkTokenAsUsedAsync(resetToken.Id);

                var newHash   = BC.HashPassword(resetRequest.NewPassword);
                await _credentialsRepository.UpdatePasswordHashAsync(foundUser.UserUid, newHash);

                foundUser.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateUserAsync(foundUser);

                return new ResetForgottenPasswordResponseDto
                {
                    UserId    = foundUser.UserId,
                    UpdatedAt = foundUser.UpdatedAt!.Value
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthService: ResetForgottenPasswordAsync - {Message}", ex.Message);
                throw new Exception($"AuthService: ResetForgottenPasswordAsync - {ex.Message}", ex);
            }
        }

    }
}
