using System;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public interface IExpression
    {
        Scope ReturningScope();
    }

    public class ExpressionTree : CodeAction, IExpression
    {
        public IExpression[] Tree { get; }
        public bool Completed { get; } = true;

        public ExpressionTree(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.ExprContext exprContext)
        {
            var exprContextTree = exprContext.expr();

            Tree = new IExpression[exprContextTree.Length];
            IExpression current = GetExpression(script, translateInfo, scope, exprContextTree[0]);
            Tree[0] = current;
            if (current != null)
                for (int i = 1; i < exprContextTree.Length; i++)
                {
                    current = GetExpression(script, translateInfo, current.ReturningScope() ?? new Scope(), exprContextTree[i]);
                    Tree[i] = current;

                    if (current == null)
                    {
                        Completed = false;
                        break;
                    }
                }
        }

        public Scope ReturningScope()
        {
            // TODO: Return player variables.
            return null;
        }
    }

    public class NumberAction : CodeAction, IExpression
    {
        public double Value { get; }

        public NumberAction(ScriptFile script, DeltinScriptParser.NumberContext numberContext)
        {
            Value = double.Parse(numberContext.GetText());
        }

        public Scope ReturningScope()
        {
            return null;
        }
    }

    public class BoolAction : CodeAction, IExpression
    {
        public bool Value { get; }

        public BoolAction(ScriptFile script, bool value)
        {
            Value = value;
        }

        public Scope ReturningScope()
        {
            return null;
        }
    }

    // TODO: Maybe combine CallVariableAction and Var?
    public class CallVariableAction : CodeAction, IExpression
    {
        public Var Calling { get; }

        public CallVariableAction(Var calling)
        {
            Calling = calling;
        }

        public Scope ReturningScope()
        {
            // TODO: Return Calling type.
            return null;
        }
    }

    public class CallMethodAction : CodeAction, IExpression, IStatement
    {
        public IMethod CallingMethod { get; }

        public CallMethodAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.MethodContext methodContext)
        {
            string methodName = methodContext.PART().GetText();
            IScopeable element = scope.GetInScope(methodName, "method", script.Diagnostics, DocRange.GetRange(methodContext.PART()));

            if (element == null)
                CallingMethod = null;
            else if (element is IMethod == false)
                script.Diagnostics.Error(methodName + " is a " + element.ScopeableType + ", not a method.", DocRange.GetRange(methodContext.PART()));
            else
                CallingMethod = (IMethod)element;
        }

        public Scope ReturningScope()
        {
            // TODO: Return CallingMethod type.
            return null;
        }
    }
}