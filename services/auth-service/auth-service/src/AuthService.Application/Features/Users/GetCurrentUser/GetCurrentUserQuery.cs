using MediatR;

namespace AuthService.Application.Features.Users.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<UserDto>;
