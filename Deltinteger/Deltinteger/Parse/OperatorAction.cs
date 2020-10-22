using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class OperatorAction : IExpression
    {
        public IExpression Left { get; private set; }
        public IExpression Right { get; private set; }
        public TypeOperation Operation { get; private set; }

        public OperatorAction(ParseInfo parseInfo, Scope scope, BinaryOperatorExpression context)
        {
            Left = parseInfo.GetExpression(scope, context.Left);
            Right = parseInfo.GetExpression(scope, context.Right);

            string op = context.Operator.Operator.Operator;
            Operation = Left?.Type()?.GetOperation(TypeOperation.TypeOperatorFromString(op), Right?.Type());
                        
            if (Operation == null)
                parseInfo.Script.Diagnostics.Error("Operator '" + op + "' cannot be applied to the types '" + Left.Type().GetNameOrVoid() + "' and '" + Right.Type().GetNameOrVoid() + "'.", context.Operator.Token.Range);
        }

        public Scope ReturningScope() => Operation?.ReturnType.GetObjectScope();
        public CodeType Type() => Operation?.ReturnType;
        public IWorkshopTree Parse(ActionSet actionSet)
        {
            IWorkshopTree left = Left.Parse(actionSet);
            IWorkshopTree right = Right.Parse(actionSet);
            return Operation.Resolve(left, right);
        }
    }
}
