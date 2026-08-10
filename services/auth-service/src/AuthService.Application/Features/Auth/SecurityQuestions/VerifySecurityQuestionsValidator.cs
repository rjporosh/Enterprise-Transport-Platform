using FluentValidation;

namespace AuthService.Application.Features.Auth.SecurityQuestions;

public sealed class VerifySecurityQuestionsValidator : AbstractValidator<VerifySecurityQuestionsCommand>
{
    public VerifySecurityQuestionsValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.QuestionAnswers).NotEmpty();
    }
}
