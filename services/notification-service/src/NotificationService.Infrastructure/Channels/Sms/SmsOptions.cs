namespace NotificationService.Infrastructure.Channels.Sms;

public sealed class SmsOptions
{
    public const string SectionName = "Sms";

    /// <summary>Twilio | GenericHttp — selects the ISmsSender implementation at startup, same "switch by config" convention as Database:Provider. See SmsSenderFactory.</summary>
    public string Provider { get; set; } = "GenericHttp";
    public string FromNumber { get; set; } = string.Empty;

    // Twilio
    public string TwilioAccountSid { get; set; } = string.Empty;
    public string TwilioAuthToken { get; set; } = string.Empty;

    // GenericHttp -- any REST SMS gateway that accepts {to, from, body} as
    // JSON and a bearer token; covers most regional/local SMS aggregators
    // that do not have a dedicated .NET SDK.
    public string? GenericHttpEndpoint { get; set; }
    public string? GenericHttpApiKey { get; set; }
}
