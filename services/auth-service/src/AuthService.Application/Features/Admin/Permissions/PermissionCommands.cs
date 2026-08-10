using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Permissions;

public sealed record CreatePermissionCommand(string Name, string Description, string Module) : IRequest<Guid>;
public sealed record UpdatePermissionCommand(Guid PermissionId, string Description, string Module) : IRequest;
public sealed record DeletePermissionCommand(Guid PermissionId) : IRequest;
public sealed record GetPermissionsQuery(string? Module) : IRequest<List<PermissionDto>>;

public sealed record PermissionDto(Guid Id, string Name, string Description, string Module, bool IsActive);
