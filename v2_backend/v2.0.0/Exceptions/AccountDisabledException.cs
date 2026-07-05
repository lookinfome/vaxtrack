namespace Vaxtrack.Exceptions
{
    // Thrown by AuthService.LoginAsync only after password verification succeeds — never before —
    // so a wrong-password guess can't be used to discover whether an account is disabled.
    public class AccountDisabledException : Exception
    {
        public string? Reason { get; }

        public AccountDisabledException(string? reason)
            : base("Your account has been disabled.")
        {
            Reason = reason;
        }
    }
}
