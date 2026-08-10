using MediatR;
using NotificationService.Application.Common.Models;
using NotificationService.Application.Features.Release;

namespace NotificationService.Application.Features.Release;

public sealed record GetReleaseInfoQuery : IRequest<Result<ReleaseInfoDto>>;
