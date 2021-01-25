using System;
using System.Diagnostics;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class OperatorAction : IExpression
    {
        public IExpression Left { get; private set; }
        public IExpression Right { get; private set; }
        public ITypeOperation Operation { get; private set; }

        public OperatorAction(ParseInfo parseInfo, Scope scope, BinaryOperatorExpression context)
        {
            Left = parseInfo.GetExpression(scope, context.Left);
            Right = parseInfo.GetExpression(scope, context.Right);

				
			string op = context.Operator.Operator.Operator;
            string op = context.Operator.Operator.Operator;
            Operation = Left.Type()?.Operations.GetOperation(TypeOperation.TypeOperatorFromString(op), Right.Type()) ?? GetDefaultOperation(op, parseInfo.TranslateInfo.Types);
                        
            if (Operation == null)
                parseInfo.Script.Diagnostics.Error("Operator '" + op + "' cannot be applied to the types '" + Left.Type().GetNameOrAny() + "' and '" + Right.Type().GetNameOrAny() + "'.", context.Operator.Token.Range);
        }

        private TypeOperation GetDefaultOperation(string op, ITypeSupplier supplier)
        {
            if (Left.Type() == null || Right.Type() == null || Left.Type().IsConstant() || Right.Type().IsConstant())
                return null;
            
            return new TypeOperation(supplier, TypeOperation.TypeOperatorFromString(op), supplier.Any());
        }

        public Scope ReturningScope() => Operation?.ReturnType.GetObjectScope();
        public CodeType Type() => Operation?.ReturnType;
        public IWorkshopTree Parse(ActionSet actionSet) => Operation.Resolve(actionSet, Left, Right);
    }
}
