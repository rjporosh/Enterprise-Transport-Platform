namespace AuthService.Application.Common.Interfaces;

/// <summary>Identity of the caller, populated from the validated JWT by the API layer.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsInRole(string role);
}
