using AuthService.Api.Security;
using AuthService.Application.Common.Interfaces;
using AuthService.Application.Features.Admin.Modules;
using AuthService.Application.Features.Admin.Permissions;
using AuthService.Application.Features.Admin.Roles;
using AuthService.Application.Features.Audit.GetAuditLogs;
using AuthService.Application.Features.Auth.ChangePassword;
using AuthService.Application.Features.Auth.ForgotPassword;
using AuthService.Application.Features.Auth.Login;
using AuthService.Application.Features.Auth.Logout;
using AuthService.Application.Features.Auth.Otp;
using AuthService.Application.Features.Auth.RefreshToken;
using AuthService.Application.Features.Auth.Register;
using AuthService.Application.Features.Auth.ResetPassword;
using AuthService.Application.Features.Auth.SecurityQuestions;
using AuthService.Application.Features.System;
using AuthService.Application.Features.Users.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthService.Api.Endpoints;

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

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .WithName("ForgotPassword")
            .WithSummary("Request a password reset token.")
            .Produces(StatusCodes.Status204NoContent)
            .RequireRateLimiting("auth-write");

        group.MapPost("/reset-password", ResetPasswordAsync)
            .WithName("ResetPassword")
            .WithSummary("Reset password using a reset token.")
            .Produces(StatusCodes.Status204NoContent)
            .RequireRateLimiting("auth-write");

        group.MapPost("/otp/request", RequestOtpAsync)
            .WithName("RequestOtp")
            .WithSummary("Request an OTP via email or SMS.")
            .Produces(StatusCodes.Status204NoContent)
            .RequireRateLimiting("auth-write");

        group.MapPost("/otp/verify", VerifyOtpAsync)
            .WithName("VerifyOtp")
            .WithSummary("Verify an OTP code.")
            .Produces(StatusCodes.Status204NoContent)
            .RequireRateLimiting("auth-write");

        group.MapPost("/security-questions/configure", ConfigureSecurityQuestionsAsync)
            .WithName("ConfigureSecurityQuestions")
            .WithSummary("Configure security questions for recovery.")
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization();

        group.MapPost("/security-questions/verify", VerifySecurityQuestionsAsync)
            .WithName("VerifySecurityQuestions")
            .WithSummary("Verify security answers for password recovery.")
            .Produces(StatusCodes.Status204NoContent)
            .RequireRateLimiting("auth-write");

        group.MapGet("/audit-logs", GetAuditLogsAsync)
            .WithName("GetAuditLogs")
            .WithSummary("Search the security audit trail (Admin only).")
            .Produces<PagedResult<AuditLogDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        var admin = app.MapGroup("/api/v1/admin").RequireAuthorization(policy => policy.RequireRole("Admin")).WithTags("Admin");

        admin.MapPost("/permissions", CreatePermissionAsync).WithName("CreatePermission").Produces<Guid>(StatusCodes.Status200OK);
        admin.MapPut("/permissions/{permissionId:guid}", UpdatePermissionAsync).WithName("UpdatePermission").Produces(StatusCodes.Status204NoContent);
        admin.MapDelete("/permissions/{permissionId:guid}", DeletePermissionAsync).WithName("DeletePermission").Produces(StatusCodes.Status204NoContent);
        admin.MapGet("/permissions", GetPermissionsAsync).WithName("GetPermissions").Produces<List<PermissionDto>>(StatusCodes.Status200OK);

        admin.MapPost("/modules", CreateModuleAsync).WithName("CreateModule").Produces<Guid>(StatusCodes.Status200OK);
        admin.MapPut("/modules/{moduleId:guid}", UpdateModuleAsync).WithName("UpdateModule").Produces(StatusCodes.Status204NoContent);
        admin.MapDelete("/modules/{moduleId:guid}", DeleteModuleAsync).WithName("DeleteModule").Produces(StatusCodes.Status204NoContent);
        admin.MapGet("/modules", GetModulesAsync).WithName("GetModules").Produces<List<ModuleDto>>(StatusCodes.Status200OK);

        admin.MapPost("/roles", CreateRoleAsync).WithName("CreateRole").Produces<Guid>(StatusCodes.Status200OK);
        admin.MapPut("/roles/{roleId:guid}", UpdateRoleAsync).WithName("UpdateRole").Produces(StatusCodes.Status204NoContent);
        admin.MapGet("/roles", GetRolesAsync).WithName("GetRoles").Produces<List<RoleDto>>(StatusCodes.Status200OK);
        admin.MapPost("/roles/{roleId:guid}/permissions", AssignPermissionAsync).WithName("AssignPermission").Produces(StatusCodes.Status204NoContent);
        admin.MapDelete("/roles/{roleId:guid}/permissions/{permissionId:guid}", RemovePermissionAsync).WithName("RemovePermission").Produces(StatusCodes.Status204NoContent);
        admin.MapPost("/users/{userId:guid}/roles", AssignRoleToUserAsync).WithName("AssignRoleToUser").Produces(StatusCodes.Status204NoContent);
        admin.MapDelete("/users/{userId:guid}/roles/{roleId:guid}", RemoveRoleFromUserAsync).WithName("RemoveRoleFromUser").Produces(StatusCodes.Status204NoContent);

        group.MapGet("/release-info", GetReleaseInfoAsync)
            .WithName("GetReleaseInfo")
            .WithSummary("SQA/testers: current Auth Service release information.")
            .Produces<ReleaseInfoResponse>(StatusCodes.Status200OK)
            .AllowAnonymous();
    }

    private static async Task<IResult> RegisterAsync([FromBody] RegisterRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Email, request.Password, request.FirstName, request.LastName, request.PhoneNumber, httpContext.GetClientIpAddress(), httpContext.GetUserAgent());
        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(TokenPairResponse.From(result));
    }

    private static async Task<IResult> LoginAsync([FromBody] LoginRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password, httpContext.GetClientIpAddress(), httpContext.GetUserAgent());
        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(TokenPairResponse.From(result));
    }

    private static async Task<IResult> RefreshAsync([FromBody] RefreshTokenRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken, httpContext.GetClientIpAddress(), httpContext.GetUserAgent());
        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(TokenPairResponse.From(result));
    }

    private static async Task<IResult> LogoutAsync([FromBody] RefreshTokenRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
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

    private static async Task<IResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request, HttpContext httpContext, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return Results.Unauthorized();
        var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword, httpContext.GetClientIpAddress(), httpContext.GetUserAgent());
        await sender.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(request.Email, httpContext.GetClientIpAddress(), httpContext.GetUserAgent());
        await sender.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ResetPasswordAsync([FromBody] ResetPasswordRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.Token, request.NewPassword, httpContext.GetClientIpAddress(), httpContext.GetUserAgent());
        await sender.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> RequestOtpAsync([FromBody] RequestOtpRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var command = new RequestOtpCommand(request.UserId, request.Channel, request.Destination, httpContext.GetClientIpAddress(), httpContext.GetUserAgent());
        await sender.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> VerifyOtpAsync([FromBody] VerifyOtpRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var command = new VerifyOtpCommand(request.UserId, request.Code, request.Channel, httpContext.GetClientIpAddress(), httpContext.GetUserAgent());
        await sender.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ConfigureSecurityQuestionsAsync([FromBody] ConfigureSecurityQuestionsRequest request, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return Results.Unauthorized();
        var command = new ConfigureSecurityQuestionsCommand(userId, request.QuestionAnswers, null, null);
        await sender.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> VerifySecurityQuestionsAsync([FromBody] VerifySecurityQuestionsRequest request, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
    {
        var command = new VerifySecurityQuestionsCommand(request.UserId, request.QuestionAnswers, httpContext.GetClientIpAddress(), httpContext.GetUserAgent());
        await sender.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetAuditLogsAsync([AsParameters] AuditLogQueryParameters query, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAuditLogsQuery(query.UserId, query.IpAddress, query.Page ?? 1, query.PageSize ?? 50), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreatePermissionAsync([FromBody] CreatePermissionRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AuthService.Application.Features.Admin.Permissions.CreatePermissionCommand(request.Name, request.Description, request.Module), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdatePermissionAsync(Guid permissionId, [FromBody] UpdatePermissionRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new AuthService.Application.Features.Admin.Permissions.UpdatePermissionCommand(permissionId, request.Description, request.Module), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> DeletePermissionAsync(Guid permissionId, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new AuthService.Application.Features.Admin.Permissions.DeletePermissionCommand(permissionId), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetPermissionsAsync([AsParameters] AuthService.Application.Features.Admin.Permissions.GetPermissionsQuery query, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateModuleAsync([FromBody] CreateModuleRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AuthService.Application.Features.Admin.Modules.CreateModuleCommand(request.Name, request.Description), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateModuleAsync(Guid moduleId, [FromBody] UpdateModuleRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new AuthService.Application.Features.Admin.Modules.UpdateModuleCommand(moduleId, request.Description), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteModuleAsync(Guid moduleId, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new AuthService.Application.Features.Admin.Modules.DeleteModuleCommand(moduleId), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetModulesAsync(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AuthService.Application.Features.Admin.Modules.GetModulesQuery(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateRoleAsync([FromBody] CreateRoleRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AuthService.Application.Features.Admin.Roles.CreateRoleCommand(request.Name, request.Description), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateRoleAsync(Guid roleId, [FromBody] UpdateRoleRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new AuthService.Application.Features.Admin.Roles.UpdateRoleCommand(roleId, request.Description), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetRolesAsync(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AuthService.Application.Features.Admin.Roles.GetRolesQuery(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> AssignPermissionAsync(Guid roleId, [FromBody] AssignPermissionRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new AuthService.Application.Features.Admin.Roles.AssignPermissionCommand(roleId, request.PermissionId), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> RemovePermissionAsync(Guid roleId, Guid permissionId, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new AuthService.Application.Features.Admin.Roles.RemovePermissionCommand(roleId, permissionId), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> AssignRoleToUserAsync(Guid userId, [FromBody] AssignRoleRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new AuthService.Application.Features.Admin.Roles.AssignRoleToUserCommand(userId, request.RoleId), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveRoleFromUserAsync(Guid userId, Guid roleId, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new AuthService.Application.Features.Admin.Roles.RemoveRoleFromUserCommand(userId, roleId), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetReleaseInfoAsync(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AuthService.Application.Features.System.GetReleaseInfoQuery(), cancellationToken);
        return Results.Ok(result);
    }
}

public sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName, string? PhoneNumber);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
public sealed record RequestOtpRequest(Guid UserId, string Channel, string Destination);
public sealed record VerifyOtpRequest(Guid UserId, string Code, string Channel);
public sealed record ConfigureSecurityQuestionsRequest(IDictionary<Guid, string> QuestionAnswers);
public sealed record VerifySecurityQuestionsRequest(Guid UserId, IDictionary<Guid, string> QuestionAnswers);
public sealed record AuditLogQueryParameters(Guid? UserId, string? IpAddress, int? Page, int? PageSize);

public sealed record CreatePermissionRequest(string Name, string Description, string Module);
public sealed record UpdatePermissionRequest(string Description, string Module);
public sealed record CreateModuleRequest(string Name, string Description);
public sealed record UpdateModuleRequest(string Description);
public sealed record CreateRoleRequest(string Name, string Description);
public sealed record UpdateRoleRequest(string Description);
public sealed record AssignPermissionRequest(Guid PermissionId);
public sealed record AssignRoleRequest(Guid RoleId);

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
