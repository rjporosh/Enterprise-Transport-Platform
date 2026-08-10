using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Admin.Modules;

public sealed record CreateModuleCommand(string Name, string Description) : IRequest<Guid>;
public sealed record UpdateModuleCommand(Guid ModuleId, string Description) : IRequest;
public sealed record DeleteModuleCommand(Guid ModuleId) : IRequest;
public sealed record GetModulesQuery : IRequest<List<ModuleDto>>;

public sealed record ModuleDto(Guid Id, string Name, string Description, bool IsActive);
