namespace AuthService.Domain.Exceptions;

public sealed class OtpRateLimitExceededException : DomainException
{
    public OtpRateLimitExceededException()
        : base("Too many OTP requests. Please try again later.") { }
}
