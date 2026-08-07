namespace NotificationService.Infrastructure.Channels.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseStartTls { get; set; } = true;
    public string FromAddress { get; set; } = "no-reply@bus-ticketing.local";
    public string FromDisplayName { get; set; } = "Bus Ticketing";
}
