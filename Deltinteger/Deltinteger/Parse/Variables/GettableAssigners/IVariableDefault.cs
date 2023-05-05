namespace Deltin.Deltinteger.Parse;
using System;

public interface IVariableDefault
{
    IWorkshopTree GetDefaultValue(ActionSet actionSet);

    public static IVariableDefault Create(Func<ActionSet, IWorkshopTree> getDefaultValue) => new VariableDefault(getDefaultValue);

    public static IVariableDefault FromExpression(IExpression expression)
    {
        if (expression == null)
            return null;
        return Create(actionSet => expression.Parse(actionSet));
    }

    record VariableDefault(Func<ActionSet, IWorkshopTree> getDefaultValue) : IVariableDefault
    {
        IWorkshopTree IVariableDefault.GetDefaultValue(ActionSet actionSet) => getDefaultValue(actionSet);
    }
}