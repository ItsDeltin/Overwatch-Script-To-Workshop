using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        public static FuncMethod ModifyVariable(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "ModifyVariable",
            Documentation = "Modifies the value of a variable.",
            Parameters = new CodeParameter[] {
                new VariableParameter("variable", "The variable to modify. Player variables will modify the event player's variable.", deltinScript.Types.Any()),
                new CodeParameter("operation", "The way in which the variable’s value will be changed. Options include standard arithmetic operations as well as array operations for appending and removing values.", deltinScript.Types.EnumType("Operation")),
                new CodeParameter("value", "The value used for the modification. For arithmetic operations, this is the second of two operands, with the other being the variable’s existing value. For array operations, this is the value to append or remove.", deltinScript.Types.Any())
            },
            Action = (actionSet, methodCall) => {
                VariableResolve variableResolve = (VariableResolve)methodCall.AdditionalParameterData[0];
                Operation operation = ((ElementEnumMember)methodCall.ParameterValues[1]).GetOperation();
                IWorkshopTree value = methodCall.ParameterValues[2];

                VariableElements variableElements = variableResolve.ParseElements(actionSet);

                variableElements.IndexReference.Modify(actionSet, operation, value, variableElements.Target, variableElements.Index);
                return null;
            }
        };
    }
}