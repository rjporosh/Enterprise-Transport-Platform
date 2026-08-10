using FluentValidation;

namespace AuthService.Application.Features.Auth.SecurityQuestions;

public sealed class ConfigureSecurityQuestionsValidator : AbstractValidator<ConfigureSecurityQuestionsCommand>
{
    public ConfigureSecurityQuestionsValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.QuestionAnswers).NotEmpty();
        RuleFor(x => x.QuestionAnswers.Count).GreaterThanOrEqualTo(3).LessThanOrEqualTo(5);
    }
}
