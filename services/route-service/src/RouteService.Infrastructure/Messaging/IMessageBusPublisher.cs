using Microsoft.Extensions.Options;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Infrastructure.Messaging;

public interface IMessageBusPublisher
{
    Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken = default);
}
