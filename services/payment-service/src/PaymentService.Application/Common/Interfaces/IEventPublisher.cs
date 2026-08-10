using MediatR;
using PaymentService.Domain.Common;

namespace PaymentService.Application.Common.Interfaces;

public interface IEventPublisher
{
    Task EnqueueAsync(INotification domainEvent, CancellationToken cancellationToken = default);
}
