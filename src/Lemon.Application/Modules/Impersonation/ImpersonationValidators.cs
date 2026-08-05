using FluentValidation;

namespace Lemon.Application.Modules.Impersonation;

public sealed class StartImpersonationRequestValidator : AbstractValidator<StartImpersonationRequest>
{
    public StartImpersonationRequestValidator()
    {
        RuleFor(x => x.TargetAdminId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(2).MaximumLength(200);
    }
}
