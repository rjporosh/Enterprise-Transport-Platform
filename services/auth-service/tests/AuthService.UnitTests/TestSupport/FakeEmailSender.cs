using AuthService.Application.Common.Interfaces;

namespace AuthService.UnitTests.TestSupport;

public sealed class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> SentEmails { get; } = new();

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        SentEmails.Add((to, subject, body));
        return Task.CompletedTask;
    }
}
