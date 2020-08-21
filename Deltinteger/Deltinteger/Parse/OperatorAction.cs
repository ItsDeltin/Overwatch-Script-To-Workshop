using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class OperatorAction : IExpression
    {
        public IExpression Left { get; private set; }
        public IExpression Right { get; private set; }
        public TypeOperation Operation { get; private set; }

        public OperatorAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_op_1Context context) {
            GetParts(parseInfo, scope, context.left, context.op.Text, DocRange.GetRange(context.op), context.right);
        }
        public OperatorAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_op_2Context context) {
            GetParts(parseInfo, scope, context.left, context.op.Text, DocRange.GetRange(context.op), context.right);
        }
        public OperatorAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_op_boolContext context) {
            GetParts(parseInfo, scope, context.left, context.BOOL().GetText(), DocRange.GetRange(context.BOOL()), context.right);
        }
        public OperatorAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_op_compareContext context) {
            GetParts(parseInfo, scope, context.left, context.op.Text, DocRange.GetRange(context.op), context.right);
        }

        private void GetParts(ParseInfo parseInfo, Scope scope, DeltinScriptParser.ExprContext left, string op, DocRange opRange, DeltinScriptParser.ExprContext right)
        {
            // Left operator.
            if (left == null) parseInfo.Script.Diagnostics.Error("Missing left operator.", opRange);
            else Left = parseInfo.GetExpression(scope, left);

            // Right operator.
            if (right == null) parseInfo.Script.Diagnostics.Error("Missing right operator.", opRange);
            else Right = parseInfo.GetExpression(scope, right);

            if (Left != null && Right != null && Left.Type() != null && Right.Type() != null)
            {
                Operation = Left.Type().GetOperation(TypeOperation.TypeOperatorFromString(op), Right.Type());
                            
                if (Operation == null)
                    parseInfo.Script.Diagnostics.Error("Operator '" + op + "' cannot be applied to the types '" + Left.Type().Name + "' and '" + Right.Type().Name + "'.", opRange);
            }
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