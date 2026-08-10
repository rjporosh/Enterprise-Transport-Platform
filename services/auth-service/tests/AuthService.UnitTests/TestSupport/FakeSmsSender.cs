using AuthService.Application.Common.Interfaces;

namespace AuthService.UnitTests.TestSupport;

public sealed class FakeSmsSender : ISmsSender
{
    public List<(string PhoneNumber, string Message)> SentMessages { get; } = new();

    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        SentMessages.Add((phoneNumber, message));
        return Task.CompletedTask;
    }
}
