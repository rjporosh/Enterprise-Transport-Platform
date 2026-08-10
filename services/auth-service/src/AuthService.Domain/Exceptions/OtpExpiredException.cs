namespace AuthService.Domain.Exceptions;

public sealed class OtpExpiredException : DomainException
{
    public OtpExpiredException()
        : base("OTP has expired. Please request a new one.") { }
}
