namespace AuthService.Domain.Exceptions;

public sealed class InvalidOtpException : DomainException
{
    public InvalidOtpException()
        : base("Invalid OTP code.") { }
}
