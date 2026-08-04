#nullable enable

namespace Deltin.Deltinteger.Parse;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>Resolves the initial value of a variable.</summary>
public interface IVariableDefault
{
    /// <summary>Resolve the initial value using the given ActionSet.</summary>
    IWorkshopTree GetDefaultValue(ActionSet actionSet);

    /// <summary>Resolve the initial value using a factory.</summary>
    public static IVariableDefault Create(Func<ActionSet, IWorkshopTree> getDefaultValue) => new VariableDefault(getDefaultValue);

    /// <summary>Resolve the iniital value using an IExpression.</summary>
    public static IVariableDefault? FromExpression(IExpression? expression)
    {
        if (expression is null)
            return null;
        return Create(expression.Parse);
    }

    /// <summary>Resolve the initial value using a workshop value.</summary>
    public static IVariableDefault? FromWorkshopValue(IWorkshopTree? value)
    {
        if (value is null)
            return null;
        return Create(_ => value);
    }

    record VariableDefault(Func<ActionSet, IWorkshopTree> getDefaultValue) : IVariableDefault
    {
        IWorkshopTree IVariableDefault.GetDefaultValue(ActionSet actionSet) => getDefaultValue(actionSet);
    }
}