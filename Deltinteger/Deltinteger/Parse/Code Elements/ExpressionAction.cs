using System;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public interface IExpression {}

    public class NumberAction : IExpression
    {
        public double Value { get; }

        public NumberAction(ScriptFile script, DeltinScriptParser.NumberContext numberContext)
        {
            Value = double.Parse(numberContext.GetText());
        }
    }

    public class BoolAction : IExpression
    {
        public bool Value { get; }

        public BoolAction(ScriptFile script, bool value)
        {
            Value = value;
        }
    }

    public class CallVariableAction : IExpression
    {
        public Var Calling { get; }

        public CallVariableAction(Var calling)
        {
            Calling = calling;
        }
    }

    public class CallMethodAction : IExpression, IStatement
    {
        public IMethod CallingMethod { get; }

        public CallMethodAction(ScriptFile script, Scope scope, DeltinScriptParser.MethodContext methodContext)
        {
            string methodName = methodContext.PART().GetText();
            IScopeable element = scope.GetInScope(methodName);

            if (element == null)
                script.Diagnostics.Error(methodName + " does not exist in the current scope.", DocRange.GetRange(methodContext));

            else if (element is IMethod == false)
                script.Diagnostics.Error(methodName + " is a " + element.ScopeableType + ", not a method.", DocRange.GetRange(methodContext));
            
            else
                CallingMethod = (IMethod)element;
        }
    }
}