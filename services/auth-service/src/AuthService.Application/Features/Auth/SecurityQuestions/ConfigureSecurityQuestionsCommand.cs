using AuthService.Application.Common.Interfaces;
using MediatR;

namespace AuthService.Application.Features.Auth.SecurityQuestions;

public sealed record ConfigureSecurityQuestionsCommand(Guid UserId, IDictionary<Guid, string> QuestionAnswers, string? IpAddress, string? UserAgent) : IRequest;
