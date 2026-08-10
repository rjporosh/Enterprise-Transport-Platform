using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Roles;

public sealed record CreateRoleCommand(string Name, string Description) : IRequest<Guid>;
public sealed record UpdateRoleCommand(Guid RoleId, string Description) : IRequest;
public sealed record DeleteRoleCommand(Guid RoleId) : IRequest;
public sealed record AssignPermissionCommand(Guid RoleId, Guid PermissionId) : IRequest;
public sealed record RemovePermissionCommand(Guid RoleId, Guid PermissionId) : IRequest;
public sealed record AssignRoleToUserCommand(Guid UserId, Guid RoleId) : IRequest;
public sealed record RemoveRoleFromUserCommand(Guid UserId, Guid RoleId) : IRequest;
public sealed record GetRolesQuery : IRequest<List<RoleDto>>;

public sealed record RoleDto(Guid Id, string Name, string Description, bool IsActive);
public sealed record RolePermissionDto(Guid PermissionId, string PermissionName, string Module);
