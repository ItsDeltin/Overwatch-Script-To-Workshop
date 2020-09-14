using System;

namespace Deltin.Deltinteger.Parse
{
    class ConstantExpressionResolver
    {
        public static void Resolve(IExpression start, Action<IExpression> callback)
        {
            Action resolver = () => {
                IExpression resolve = null;

                // If the expression is a CallvariableAction, resolve the initial value.
                if (start is CallVariableAction callVariableAction)
                {
                    if (callVariableAction.Calling is Var var)
                        resolve = var.InitialValue;
                }

                // If the expression is a function with only one return statement, resolve the value being returned.
                else if (start is CallMethodAction callMethod)
                {
                    // If the function is calling a DefinedMethod, resolve the value.
                    if (callMethod.CallingMethod is DefinedMethod definedMethod && definedMethod.SingleReturnValue != null)
                        resolve = definedMethod.SingleReturnValue;
                    
                    // If the expression is a parametered macro, resolve the value.
                    else if (callMethod.CallingMethod is DefinedMacro definedMacro)
                        resolve = definedMacro.Expression;
                }

                // If the expression is a macro variable, resolve the value.
                else if (start is MacroVar macroVar)
                    resolve = macroVar.Expression;
                
                // If the expression is an ExpressionTree, resolve the last value.
                else if (start is ExpressionTree expressionTree)
                    resolve = expressionTree.Result;
                
                if (resolve == null) callback.Invoke(start);
                else Resolve(resolve, callback);
            };

            if (start is IBlockListener blockListener)
                blockListener.OnBlockApply(new OnBlockApplied(() => resolver.Invoke()));
            else
                resolver.Invoke();
        }
    }

    class OnBlockApplied : IOnBlockApplied
    {
        private readonly Action _action;

        public OnBlockApplied(Action action)
        {
            _action = action;
        }

        public void Applied() => _action.Invoke();
    }
}