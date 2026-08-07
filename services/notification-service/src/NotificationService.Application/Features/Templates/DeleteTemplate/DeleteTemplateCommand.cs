using MediatR;
using NotificationService.Application.Common.Models;

namespace NotificationService.Application.Features.Templates.DeleteTemplate;

public sealed record DeleteTemplateCommand(Guid TemplateId) : IRequest<Result>;
