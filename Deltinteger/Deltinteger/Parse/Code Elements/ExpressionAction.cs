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
        public IExpression Result { get; }
        public bool Completed { get; } = true;
        private DeltinScript translateInfo { get; }
        public DeltinScriptParser.ExprContext[] ExprContextTree { get; }

        public ExpressionTree(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.ExprContext exprContext)
        {
            this.translateInfo = translateInfo;
            ExprContextTree = exprContext.expr();

            Tree = new IExpression[ExprContextTree.Length];
            IExpression current = GetExpression(script, translateInfo, scope, ExprContextTree[0]);
            Tree[0] = current;
            if (current != null)
                for (int i = 1; i < ExprContextTree.Length; i++)
                {
                    current = GetExpression(script, translateInfo, current.ReturningScope() ?? new Scope(), ExprContextTree[i]);

                    if (current is CallVariableAction == false && current is CallMethodAction == false)
                        script.Diagnostics.Error("Expected variable or method.", DocRange.GetRange(ExprContextTree[i]));

                    Tree[i] = current;

                    if (current == null)
                    {
                        Completed = false;
                        break;
                    }
                }
            else Completed = false;
        
            if (Completed)
                Result = Tree[Tree.Length - 1];
        }

        public Scope ReturningScope()
        {
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
        private DeltinScript translateInfo { get; }

        public CallVariableAction(DeltinScript translateInfo, Var calling)
        {
            this.translateInfo = translateInfo;
            Calling = calling;
        }

        public Scope ReturningScope()
        {
            // TODO: Should all variables have a type?
            // Instead of the default type being null, it is a class that derived from CodeType?
            if (Calling.Type == null)
                return translateInfo.PlayerVariableScope;
            else
                return Calling.Type.GetObjectScope() ?? translateInfo.PlayerVariableScope;
        }
    }

    public class ArrayAction : CodeAction, IExpression
    {
        public IExpression Expression { get; }
        public IExpression Index { get; }

        public ArrayAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.ExprContext exprContext)
        {
            if (exprContext.expr() == null) throw new Exception("Array can be determined without inital expression.");

            Expression = GetExpression(script, translateInfo, scope, exprContext.expr(0));

            if (exprContext.index == null)
                script.Diagnostics.Error("Expected an expression.", DocRange.GetRange(exprContext.INDEX_START()));
            else
                Index = GetExpression(script, translateInfo, scope, exprContext.index);
        }

        public Scope ReturningScope()
        {
            // TODO: Support class arrays.
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