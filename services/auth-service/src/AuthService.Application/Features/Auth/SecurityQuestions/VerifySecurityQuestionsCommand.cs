using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Auth.SecurityQuestions;

public sealed record VerifySecurityQuestionsCommand(Guid UserId, IDictionary<Guid, string> QuestionAnswers, string? IpAddress, string? UserAgent) : IRequest;
