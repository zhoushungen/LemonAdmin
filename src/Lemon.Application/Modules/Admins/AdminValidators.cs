using FluentValidation;

namespace Lemon.Application.Modules.Admins;

public sealed class CreateAdminRequestValidator : AbstractValidator<CreateAdminRequest>
{
    public CreateAdminRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Password).MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.RoleId).NotNull().WithMessage("普通管理员必须选择角色");
        RuleFor(x => x.DepartmentId).NotNull().WithMessage("普通管理员必须选择主部门");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class UpdateAdminRequestValidator : AbstractValidator<UpdateAdminRequest>
{
    public UpdateAdminRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
