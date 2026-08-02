namespace AuthService.Domain.Enums;

/// <summary>Every action the audit trail records — see AuditLog and IAuditLogger. Kept as a closed enum (not a free-text string) so Grafana/queries can filter reliably.</summary>
public enum AuditAction
{
    Register = 0,
    LoginSuccess = 1,
    LoginFailure = 2,
    AccountLockedOut = 3,
    TokenRefresh = 4,
    TokenReuseDetected = 5,
    Logout = 6,
    PasswordChanged = 7
}
