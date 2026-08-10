namespace AuthService.Application.Features.System;

public sealed record GetReleaseInfoQuery() : MediatR.IRequest<ReleaseInfoResponse>;
