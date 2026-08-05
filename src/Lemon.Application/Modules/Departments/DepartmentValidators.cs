using FluentValidation;

namespace Lemon.Application.Modules.Departments;

public sealed class SaveDepartmentRequestValidator : AbstractValidator<SaveDepartmentRequest>
{
    public SaveDepartmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Code).NotEmpty().Matches("^[A-Za-z][A-Za-z0-9_-]{1,79}$");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
