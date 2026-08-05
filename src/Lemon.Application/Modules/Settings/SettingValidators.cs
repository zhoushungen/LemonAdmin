using System.Globalization;
using System.Text.Json;
using FluentValidation;

namespace Lemon.Application.Modules.Settings;

public sealed class UpsertSettingRequestValidator : AbstractValidator<UpsertSettingRequest>
{
    private static readonly string[] Types = ["string", "int", "bool", "decimal", "json"];

    public UpsertSettingRequestValidator()
    {
        RuleFor(x => x.SettingGroup).NotEmpty().MaximumLength(80);
        RuleFor(x => x.SettingValue).NotNull().MaximumLength(4000);
        RuleFor(x => x.ValueType).Must(x => Types.Contains(x)).WithMessage("不支持的配置类型");
        RuleFor(x => x).Must(HasValidValue).WithMessage("配置值与配置类型不匹配");
    }

    private static bool HasValidValue(UpsertSettingRequest request)
    {
        var value = request.SettingValue;
        return request.ValueType switch
        {
            "string" => true,
            "int" => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "bool" => bool.TryParse(value, out _),
            "decimal" => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
            "json" => IsJson(value),
            _ => false
        };
    }

    private static bool IsJson(string value)
    {
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
