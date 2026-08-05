using FluentValidation;

namespace Lemon.Application.Modules.Auth;

public sealed class AdminLoginRequestValidator : AbstractValidator<AdminLoginRequest>
{
    public AdminLoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(128);
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}
