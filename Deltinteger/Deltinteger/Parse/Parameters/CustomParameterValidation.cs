using System;
using Deltin.Deltinteger.Compiler;
namespace Deltin.Deltinteger.Parse;

#nullable enable

/// <summary>
/// Functions exactly like CodeParameter with an additional custom validation function.
/// </summary>
class CustomParameterValidation : CodeParameter
{
    readonly Func<CustomValidationParams, object>? validator;

    public CustomParameterValidation(
        string name,
        MarkupBuilder documentation,
        ICodeTypeSolver type,
        Func<CustomValidationParams, object>? validator) : base(name, documentation, type)
    {
        this.validator = validator;
    }

    public override object? Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
    {
        // CodeParameter Validate always returns null, no need to worry about the value.
        base.Validate(parseInfo, value, valueRange, additionalData);
        return validator?.Invoke(new(parseInfo, value, valueRange, additionalData));
    }

    public record struct CustomValidationParams(ParseInfo ParseInfo, IExpression Value, DocRange ValueRange, object AdditionalData);
}