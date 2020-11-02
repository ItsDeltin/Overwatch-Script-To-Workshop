using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("ModifyVariable", "Modifies a variable.", CustomMethodType.Action, typeof(NullType))]
    class ModifyVariable : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new VariableParameter("variable", "The variable to modify. Player variables will modify the event player's variable."),
            new CodeParameter("operation", "The way in which the variable’s value will be changed. Options include standard arithmetic operations as well as array operations for appending and removing values.", ValueGroupType.GetEnumType("Operation")),
            new CodeParameter("value", "The value used for the modification. For arithmetic operations, this is the second of two operands, with the other being the variable’s existing value. For array operations, this is the value to append or remove.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            VariableResolve variableResolve = (VariableResolve)additionalParameterData[0];
            Operation operation = ((ElementEnumMember)parameterValues[1]).GetOperation();
            Element value = (Element)parameterValues[2];

            VariableElements variableElements = variableResolve.ParseElements(actionSet);

            actionSet.AddAction(variableElements.IndexReference.ModifyVariable(
                operation, value, variableElements.Target, variableElements.Index
            ));
            return null;
        }
    }
}
