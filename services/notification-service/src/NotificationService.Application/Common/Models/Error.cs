namespace NotificationService.Application.Common.Models;

/// <summary>
/// A single structured error, shaped to serialize directly into the
/// platform's standard API error contract (see CLAUDE.md, "API Response
/// Standard" and "Result Pattern"):
/// <code>{ "code": "...", "field": "...", "message": "..." }</code>
/// Field is null for errors that aren't tied to one input (business-rule
/// or not-found failures); populated for per-property validation failures
/// so the frontend can highlight the exact form field.
/// </summary>
public sealed record Error(string Code, string Message, string? Field = null)
{
    public static Error Validation(string field, string message) => new("VALIDATION_ERROR", message, field);
    public static Error NotFound(string message) => new("NOT_FOUND", message);
    public static Error Conflict(string message) => new("CONFLICT", message);
    public static Error InvalidState(string message) => new("INVALID_STATE", message);
    public static Error Unexpected(string message) => new("UNEXPECTED_ERROR", message);
}
