using AuthService.Api.Security;
using AuthService.Application.Common.Interfaces;
using AuthService.Application.Features.Audit.GetAuditLogs;
using AuthService.Application.Features.Auth.ChangePassword;
using AuthService.Application.Features.Auth.Login;
using AuthService.Application.Features.Auth.Logout;
using AuthService.Application.Features.Auth.Register;
using AuthService.Application.Features.Auth.RefreshToken;
using AuthService.Application.Features.Users.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthService.Api.Endpoints;

/// <summary>
/// All auth endpoints in one place, minimal-API style. This is the file to
/// mirror for a new vertical slice — see
/// docs/development/how-to-add-a-new-crud-endpoint.md, which walks through
/// adding GetCurrentUser (below) end to end.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Create a new account and sign in immediately.")
            .Produces<TokenPairResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            // 10 registrations/minute per IP — cheap to abuse otherwise (mass
            // account creation, email-bombing via the welcome email).
            .RequireRateLimiting("auth-write");

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Sign in with email and password.")
            .Produces<TokenPairResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status423Locked)
            .RequireRateLimiting("auth-write");

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshToken")
            .WithSummary("Exchange a refresh token for a new access/refresh token pair.")
            .Produces<TokenPairResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireRateLimiting("auth-write");

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .WithSummary("Revoke a refresh token.")
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .WithSummary("The signed-in user's profile.")
            .Produces<UserDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapPost("/change-password", ChangePasswordAsync)
            .WithName("ChangePassword")
            .WithSummary("Change the signed-in user's password.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        // Admin-only audit trail read — the CRUD "R" over the audit_logs
        // table described in docs/architecture/auth-service-architecture.md,
        // "Audit trail". Filterable by user or IP for incident response.
        group.MapGet("/audit-logs", GetAuditLogsAsync)
            .WithName("GetAuditLogs")
            .WithSummary("Search the security audit trail (Admin only).")
            .Produces<PagedResult<AuditLogDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.Email, request.Password, request.FirstName, request.LastName, request.PhoneNumber,
            httpContext.GetClientIpAddress(), httpContext.GetUserAgent());

        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(TokenPairResponse.From(result));
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password, httpContext.GetClientIpAddress(), httpContext.GetUserAgent());
        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(TokenPairResponse.From(result));
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshTokenRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken, httpContext.GetClientIpAddress(), httpContext.GetUserAgent());
        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(TokenPairResponse.From(result));
    }

    private static async Task<IResult> LogoutAsync(
        [FromBody] RefreshTokenRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new LogoutCommand(request.RefreshToken, httpContext.GetClientIpAddress(), httpContext.GetUserAgent());
        await sender.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentUserAsync(ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return Results.Unauthorized();

        var result = await sender.Send(new GetCurrentUserQuery(userId), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return Results.Unauthorized();

        var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword, httpContext.GetClientIpAddress(), httpContext.GetUserAgent());
        await sender.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetAuditLogsAsync(
        [AsParameters] AuditLogQueryParameters query,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAuditLogsQuery(query.UserId, query.IpAddress, query.Page ?? 1, query.PageSize ?? 50), cancellationToken);
        return Results.Ok(result);
    }
}

public sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName, string? PhoneNumber);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record AuditLogQueryParameters(Guid? UserId, string? IpAddress, int? Page, int? PageSize);

public sealed record TokenPairResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    Guid UserId,
    string Email,
    IReadOnlyCollection<string> Roles)
{
    public static TokenPairResponse From(Application.Common.Models.TokenPairDto dto) =>
        new(dto.AccessToken, dto.AccessTokenExpiresAtUtc, dto.RefreshToken, dto.RefreshTokenExpiresAtUtc, dto.UserId, dto.Email, dto.Roles);
}
