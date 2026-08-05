using FluentValidation;

namespace Lemon.Application.Modules.Roles;

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Matches("^[a-z][a-z0-9_]{1,79}$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.DataScope).IsInEnum();
    }
}

public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.DataScope).IsInEnum();
    }
}
