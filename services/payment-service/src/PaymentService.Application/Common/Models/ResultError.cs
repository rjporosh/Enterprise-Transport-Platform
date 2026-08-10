namespace PaymentService.Application.Common.Models;

public sealed record ResultError(string Code, string? Field, string Message);
