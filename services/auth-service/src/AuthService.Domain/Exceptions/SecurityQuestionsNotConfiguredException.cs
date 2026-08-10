namespace AuthService.Domain.Exceptions;

public sealed class SecurityQuestionsNotConfiguredException : DomainException
{
    public SecurityQuestionsNotConfiguredException()
        : base("Security questions have not been configured for this account.") { }
}
