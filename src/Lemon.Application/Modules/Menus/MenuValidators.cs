using FluentValidation; namespace Lemon.Application.Modules.Menus;
public sealed class SaveMenuRequestValidator:AbstractValidator<SaveMenuRequest>{public SaveMenuRequestValidator(){RuleFor(x=>x.Name).NotEmpty().MaximumLength(80);RuleFor(x=>x.MenuType).Must(x=>x is "directory" or "page" or "button");}}
