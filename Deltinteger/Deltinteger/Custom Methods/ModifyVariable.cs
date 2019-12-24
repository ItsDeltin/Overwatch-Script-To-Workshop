using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.CustomMethods
{
    // TODO: New ModifyVariable overload with a player parameter.

    [CustomMethod("ModifyVariable", "Modifies a variable.", CustomMethodType.Action)]
    class ModifyVariable : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new VariableParameter("variable", "The variable to modify."),
            new CodeParameter("operation", "The way in which the variable’s value will be changed. Options include standard arithmetic operations as well as array operations for appending and removing values.", WorkshopEnumType.GetEnumType<Operation>()),
            new CodeParameter("value", "The value used for the modification. For arithmetic operations, this is the second of two operands, with the other being the variable’s existing value. For array operations, this is the value to append or remove.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            CallVariableAction callVariable = (CallVariableAction)additionalParameterData[0];
            IndexReference indexReference = (IndexReference)actionSet.IndexAssigner[callVariable.Calling];

            Operation operation = (Operation)((EnumMember)parameterValues[1]).Value;
            Element value = (Element)parameterValues[2];

            actionSet.AddAction(indexReference.ModifyVariable(operation, value, null, callVariable.ParseIndex(actionSet)));
            return null;
        }
    }
}