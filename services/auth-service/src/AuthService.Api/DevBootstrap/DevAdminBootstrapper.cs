using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.DevBootstrap;

/// <summary>
/// Development-only, idempotent seed for a guaranteed-working Admin account.
/// There is otherwise no way to reach any [RequireRole("Admin")] endpoint on
/// a fresh database — registration always creates a Customer, and nothing
/// else ever assigns the Admin role. See ai-handover.md "Admin login" for
/// the full trace of why "Unable to sign in right now." was previously
/// unfixable from the frontend side alone (there was no account to sign in
/// with, not a broken login path).
///
/// Safety:
/// - Only runs when BOOTSTRAP_ADMIN_ENABLED=true is explicitly set (checked
///   in Program.cs before this is even called; also re-checked here as a
///   second guard so this class is never accidentally unsafe if called from
///   somewhere else later).
/// - Password is hashed with the same IPasswordHasher (PBKDF2) the real
///   login path verifies against -- never stored or compared in plaintext.
/// - Idempotent: re-running does nothing if the user already exists. Never
///   touches an existing user's password or role assignments, so it can't
///   clobber someone who later changed the dev admin's password by hand.
/// - Only ever creates/looks up the "Admin" role by its well-known name --
///   never creates a duplicate role row (Role.Name has a unique index, and
///   this code checks first anyway).
/// </summary>
public static class DevAdminBootstrapper
{
    public static async Task RunAsync(IServiceProvider services, IConfiguration configuration, ILogger logger, CancellationToken cancellationToken = default)
    {
        var enabled = configuration.GetValue<bool>("BOOTSTRAP_ADMIN_ENABLED");
        if (!enabled)
        {
            logger.LogInformation("Dev admin bootstrap skipped (BOOTSTRAP_ADMIN_ENABLED is not true).");
            return;
        }

        var email = (configuration["BOOTSTRAP_ADMIN_EMAIL"] ?? "admin@bus.local").Trim().ToLowerInvariant();
        var password = configuration["BOOTSTRAP_ADMIN_PASSWORD"] ?? "Admin@12345!";

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAuthDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == Role.WellKnown.Admin, cancellationToken);
        if (adminRole is null)
        {
            // Defensive only -- RoleConfiguration.HasData seeds this via
            // migration, so in practice this branch shouldn't run. If it
            // ever does (e.g. a future migration removes the seed), fail
            // loudly instead of silently proceeding with no role to assign.
            logger.LogWarning("Dev admin bootstrap: 'Admin' role not found in database. Skipping -- check that migrations have run.");
            return;
        }

        var existingUser = await context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (existingUser is not null)
        {
            var alreadyAdmin = existingUser.UserRoles.Any(ur => ur.RoleId == adminRole.Id);
            if (!alreadyAdmin)
            {
                existingUser.AssignRole(adminRole.Id, clock.UtcNow);
                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Dev admin bootstrap: granted existing user {Email} the Admin role.", email);
            }
            else
            {
                logger.LogInformation("Dev admin bootstrap: {Email} already exists with the Admin role. Nothing to do.", email);
            }
            return;
        }

        var passwordHash = passwordHasher.Hash(password);
        var user = User.Register(Guid.NewGuid(), email, passwordHash, "Dev", "Admin", phoneNumber: null, clock.UtcNow);
        user.AssignRole(adminRole.Id, clock.UtcNow);

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Dev admin bootstrap: created development admin account {Email}. " +
            "This is a development-only convenience -- BOOTSTRAP_ADMIN_ENABLED must never be set to true in production.",
            email);
    }
}
