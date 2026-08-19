using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.System;

public sealed class GetReleaseInfoHandler : MediatR.IRequestHandler<GetReleaseInfoQuery, ReleaseInfoResponse>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GetReleaseInfoHandler> _logger;

    public GetReleaseInfoHandler(IConfiguration configuration, ILogger<GetReleaseInfoHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<ReleaseInfoResponse> Handle(GetReleaseInfoQuery _, CancellationToken cancellationToken)
    {
        var version = _configuration["Release:Version"] ?? "1.0.0";
        var releaseId = _configuration["Release:ReleaseId"] ?? "20260810-001";

        var response = new ReleaseInfoResponse(
            ServiceName: "AuthService",
            Version: version,
            ReleaseId: releaseId,
            ReleaseDate: DateTimeOffset.UtcNow,
            NewFeatures: new List<string>
            {
                "User registration and login with JWT access tokens",
                "Refresh token rotation with token-family revocation",
                "OTP generation and verification (email and SMS channels)",
                "Forgot password and reset password flows",
                "Security question configuration and verification (3 questions required)",
                "Change password with current password validation",
                "Password history enforcement (last 3 passwords cannot be reused)",
                "Role-based authorization (Customer, Operator, Admin)",
                "Permission and module management",
                "Multi-tenant context support (TenantId, CompanyId, OrganizationId, BranchId)",
                "Account lockout after 5 failed login attempts",
                "Rate limiting on sensitive endpoints",
                "Correlation ID propagation",
                "Centralized error handling with Result pattern",
                "Localization (English and Bangla)",
                "Database provider abstraction (PostgreSQL, SQL Server, MySQL)",
                "Transactional outbox pattern for event publishing",
                "Structured logging with Serilog",
                "Health checks (liveness, readiness, database, Redis, RabbitMQ)"
            },
            ChangedFeatures: new List<string>
            {
                "CORS policy added (AllowConfiguredOrigins, reads Cors:AllowedOrigins) to match booking-service and payment-service"
            },
            BugFixes: new List<string>
            {
                "Reset-password token lookup used IPasswordHasher.Hash() (PBKDF2, random salt per call) instead of the deterministic ITokenService.HashRefreshToken() used to store the token, so every /auth/reset-password call failed with InvalidResetTokenException regardless of a valid token. Fixed to hash the lookup the same way it was stored."
            },
            ApiChanges: new List<string>
            {
                "POST /api/v1/auth/register",
                "POST /api/v1/auth/login",
                "POST /api/v1/auth/refresh",
                "POST /api/v1/auth/logout",
                "GET /api/v1/auth/me",
                "POST /api/v1/auth/change-password",
                "POST /api/v1/auth/forgot-password",
                "POST /api/v1/auth/reset-password",
                "POST /api/v1/auth/otp/request",
                "POST /api/v1/auth/otp/verify",
                "POST /api/v1/auth/security-questions/configure",
                "POST /api/v1/auth/security-questions/verify",
                "GET /api/v1/admin/permissions",
                "POST /api/v1/admin/modules",
                "GET /api/v1/admin/roles",
                "POST /api/v1/admin/roles",
                "GET /api/v1/auth/audit-logs",
                "GET /api/v1/auth/release-info",
                "GET /health",
                "GET /metrics"
            },
            DatabaseChanges: new List<string>
            {
                "Added permissions, modules, policies, claims tables",
                "Added otp_records table for OTP storage",
                "Added security_questions, security_answers, user_security_questions tables",
                "Added password_histories table",
                "Added password_reset_tokens table",
                "Added user_sessions table",
                "Added user_claims, role_permissions, module_permissions tables",
                "Added is_active column to roles table"
            },
            MigrationsRequired: new List<string> { "20260810134211_AddSecurityAdminFeatures" },
            ConfigurationChanges: new List<string>
            {
                "Jwt:SigningKey must be set to a strong secret in production",
                "Database:Provider can be Postgres, SqlServer, or MySql",
                "RabbitMq:HostName, Port, UserName, Password for event publishing",
                "Redis:ConnectionString for caching and token denylist",
                "Cors:AllowedOrigins (string array) — new, defaults to http://localhost:4200 and http://localhost:5173 in appsettings.json"
            },
            TestingNotes: "Unit tests cover login, lockout, OTP, security questions (case-insensitive and whitespace normalization), password history, and admin CRUD. Integration tests require Docker (Postgres, RabbitMQ, Redis).",
            BreakingChanges: new List<string>(),
            KnownLimitations: new List<string>
            {
                "gRPC service requires Grpc.Tools code generation verification",
                "Pomelo.EntityFrameworkCore.MySql 9.0.0 has a version mismatch with EF Core 10 (NU1608 suppressed)",
                "Email and SMS senders are no-op implementations (log only); replace with real providers in production",
                "Integration tests require Docker daemon"
            });
        return Task.FromResult(response);
    }
}
