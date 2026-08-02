using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Application.Features.Users.GetCurrentUser;

/// <summary>
/// Reference implementation of the simplest possible query handler — see
/// docs/development/how-to-add-a-new-crud-endpoint.md, which walks through
/// building this exact feature step by step for onboarding developers.
/// </summary>
public sealed class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly IAuthDbContext _context;

    public GetCurrentUserHandler(IAuthDbContext context) => _context = context;

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            throw new UserNotFoundException(request.UserId);

        return new UserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.IsEmailVerified,
            user.CreatedAtUtc,
            user.LastLoginAtUtc,
            user.UserRoles.Select(ur => ur.Role.Name).ToList());
    }
}
