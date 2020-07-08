namespace Deltin.Deltinteger.Parse
{
    class ConstantExpressionResolver
    {
        public static IExpression Resolve(IExpression start)
        {
            // If the expression is a CallvariableAction, resolve the initial value.
            if (start is CallVariableAction callVariableAction)
            {
                if (callVariableAction.Calling is Var var)
                    return Resolve(var.InitialValue);
            }

            // If the expression is a function with only one return statement, resolve the value being returned.
            else if (start is CallMethodAction callMethod)
            {
                // If the function is calling a DefinedMethod, resolve the value.
                if (callMethod.CallingMethod is DefinedMethod definedMethod && definedMethod.SingleReturnValue != null)
                    return Resolve(definedMethod.SingleReturnValue);
                
                // If the expression is a parametered macro, resolve the value.
                else if (callMethod.CallingMethod is DefinedMacro definedMacro)
                    return Resolve(definedMacro.Expression);
            }

            // If the expression is a macro variable, resolve the value.
            else if (start is MacroVar macroVar)
                return Resolve(macroVar.Expression);
                        
            return start;
        }
    }
}