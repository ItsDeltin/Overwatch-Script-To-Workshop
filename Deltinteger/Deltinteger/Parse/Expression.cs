using System;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    public interface IExpression
    {
        Scope ReturningScope();
        CodeType Type();
        IWorkshopTree Parse(ActionSet actionSet, bool asElement = true);
    }

    public class NumberAction : IExpression
    {
        public double Value { get; }

        public NumberAction(ScriptFile script, DeltinScriptParser.NumberContext numberContext)
        {
            Value = double.Parse(numberContext.GetText());
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            return new V_Number(Value);
        }
    }

    public class BoolAction : IExpression
    {
        public bool Value { get; }

        public BoolAction(ScriptFile script, bool value)
        {
            Value = value;
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            if (Value) return new V_True();
            else return new V_False();
        }
    }

    public class NullAction : IExpression
    {
        public NullAction() {}
        public Scope ReturningScope() => null;
        public CodeType Type() => null;

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            return new V_Null();
        }
    }

    public class ValueInArrayAction : IExpression
    {
        public IExpression Expression { get; }
        public IExpression Index { get; }
        private DocRange expressionRange { get; }
        private DocRange indexRange { get; }
        private ParseInfo parseInfo { get; }

        public ValueInArrayAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_array_indexContext exprContext)
        {
            Expression = DeltinScript.GetExpression(parseInfo, scope, exprContext.array);
            expressionRange = DocRange.GetRange(exprContext.array);
            this.parseInfo = parseInfo;

            if (exprContext.index == null)
                parseInfo.Script.Diagnostics.Error("Expected an expression.", DocRange.GetRange(exprContext.INDEX_START()));
            else
            {
                Index = DeltinScript.GetExpression(parseInfo, scope, exprContext.index);
                indexRange = DocRange.GetRange(exprContext.index);
            }
        }

        public Scope ReturningScope() => Type()?.GetObjectScope() ?? parseInfo.TranslateInfo.PlayerVariableScope;
        public CodeType Type() => (Expression.Type() as ArrayType)?.ArrayOfType;

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            return Element.Part<V_ValueInArray>(Expression.Parse(actionSet.New(expressionRange)), Index.Parse(actionSet.New(indexRange)));
        }
    }

    public class CreateArrayAction : IExpression
    {
        public IExpression[] Values { get; }

        public CreateArrayAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.CreatearrayContext createArrayContext)
        {
            Values = new IExpression[createArrayContext.expr().Length];
            for (int i = 0; i < Values.Length; i++)
                Values[i] = DeltinScript.GetExpression(parseInfo, scope, createArrayContext.expr(i));
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            IWorkshopTree[] asWorkshop = new IWorkshopTree[Values.Length];
            for (int i = 0; i < asWorkshop.Length; i++)
                asWorkshop[i] = Values[i].Parse(actionSet);

            return Element.CreateArray(asWorkshop);
        }
    }

    public class TypeConvertAction : IExpression
    {
        public IExpression Expression { get; }
        public CodeType ConvertingTo { get; }

        public TypeConvertAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.TypeconvertContext typeConvert)
        {
            Expression = DeltinScript.GetExpression(parseInfo, scope, typeConvert.expr());

            // Get the type. Syntax error if there is none.
            if (typeConvert.PART() == null)
                parseInfo.Script.Diagnostics.Error("Expected type name.", DocRange.GetRange(typeConvert.LESS_THAN()));
            else
                ConvertingTo = parseInfo.TranslateInfo.GetCodeType(typeConvert.PART().GetText(), parseInfo.Script.Diagnostics, DocRange.GetRange(typeConvert.PART()));
        }

        public Scope ReturningScope() => ConvertingTo?.GetObjectScope();
        public CodeType Type() => ConvertingTo;
        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true) => Expression.Parse(actionSet);
    }

    public class NotAction : IExpression
    {
        public IExpression Expression { get; }

        public NotAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.ExprContext exprContext)
        {
            Expression = DeltinScript.GetExpression(parseInfo, scope, exprContext);
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;
        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true) => Element.Part<V_Not>(Expression.Parse(actionSet));
    }
    
    public class InverseAction : IExpression
    {
        public IExpression Expression { get; }

        public InverseAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.ExprContext exprContext)
        {
            Expression = DeltinScript.GetExpression(parseInfo, scope, exprContext);
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;
        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true) => Element.Part<V_Subtract>(new V_Number(0), Expression.Parse(actionSet));
    }
    
    public class OperatorAction : IExpression
    {
        public IExpression Left { get; private set; }
        public IExpression Right { get; private set; }
        public string Operator { get; private set; }

        public OperatorAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_op_1Context context)
        {
            GetParts(parseInfo, scope, context.left, context.op.Text, DocRange.GetRange(context.op), context.right);
        }
        public OperatorAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_op_2Context context)
        {
            GetParts(parseInfo, scope, context.left, context.op.Text, DocRange.GetRange(context.op), context.right);
        }
        public OperatorAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_op_boolContext context)
        {
            GetParts(parseInfo, scope, context.left, context.BOOL().GetText(), DocRange.GetRange(context.BOOL()), context.right);
        }
        public OperatorAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_op_compareContext context)
        {
            GetParts(parseInfo, scope, context.left, context.op.Text, DocRange.GetRange(context.op), context.right);
        }

        private void GetParts(ParseInfo parseInfo, Scope scope, DeltinScriptParser.ExprContext left, string op, DocRange opRange, DeltinScriptParser.ExprContext right)
        {
            // Left operator.
            if (left == null) parseInfo.Script.Diagnostics.Error("Missing left operator.", opRange);
            else Left = DeltinScript.GetExpression(parseInfo, scope, left);

            // Right operator.
            if (right == null) parseInfo.Script.Diagnostics.Error("Missing right operator.", opRange);
            else Right = DeltinScript.GetExpression(parseInfo, scope, right);

            Operator = op;
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;
        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            var left = Left.Parse(actionSet);
            var right = Right.Parse(actionSet);
            switch (Operator)
            {
                case "^": return Element.Part<V_RaiseToPower>(left,right);
                case "*": return Element.Part<V_Multiply>(left,right);
                case "/": return Element.Part<V_Divide>(left,right);
                case "%": return Element.Part<V_Modulo>(left,right);
                case "+": return Element.Part<V_Add>(left,right);
                case "-": return Element.Part<V_Subtract>(left,right);
                case "<": return new V_Compare(left, Operators.LessThan, right);
                case "<=": return new V_Compare(left, Operators.LessThanOrEqual, right);
                case "==": return new V_Compare(left, Operators.Equal, right);
                case ">=": return new V_Compare(left, Operators.GreaterThanOrEqual, right);
                case ">": return new V_Compare(left, Operators.GreaterThan, right);
                case "!=": return new V_Compare(left, Operators.NotEqual, right);
                case "&&": return Element.Part<V_And>(left,right);
                case "||": return Element.Part<V_Or>(left,right);
                default: throw new Exception($"Unrecognized operator {Operator}.");
            }
        }
    }

    public class TernaryConditionalAction : IExpression
    {
        public IExpression Condition { get; }
        public IExpression Consequent { get; }
        public IExpression Alternative { get; }
        private ParseInfo parseInfo { get; }

        public TernaryConditionalAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_ternary_conditionalContext ternaryContext)
        {
            this.parseInfo = parseInfo;
            Condition = DeltinScript.GetExpression(parseInfo, scope, ternaryContext.condition);

            if (ternaryContext.consequent == null)
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(ternaryContext.TERNARY()));
            else
                Consequent = DeltinScript.GetExpression(parseInfo, scope, ternaryContext.consequent);
            
            if (ternaryContext.alternative == null)
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(ternaryContext.TERNARY_ELSE()));
            else
                Alternative = DeltinScript.GetExpression(parseInfo, scope, ternaryContext.alternative);
        }

        public Scope ReturningScope() => Type()?.GetObjectScope() ?? parseInfo.TranslateInfo.PlayerVariableScope;
        public CodeType Type()
        {
            // Consequent or Alternative can equal null on GetExpression failure.
            if (Consequent != null && Alternative != null && Consequent.Type() == Alternative.Type()) return Consequent.Type();
            return null;
        }
        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true) => Element.TernaryConditional(Condition.Parse(actionSet), Consequent.Parse(actionSet), Alternative.Parse(actionSet));
    }

    public class RootAction : IExpression
    {
        private DeltinScript DeltinScript { get; }

        public RootAction(DeltinScript deltinScript)
        {
            DeltinScript = deltinScript;
        }

        public Scope ReturningScope() => DeltinScript.RulesetScope;
        public CodeType Type() => null;
        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true) => null;
    }

    public class ThisAction : IExpression
    {
        private CodeType ThisType { get; }

        public ThisAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_thisContext context)
        {
            ThisType = scope.GetThis();
            if (ThisType == null)
                parseInfo.Script.Diagnostics.Error("Keyword 'this' cannot be used here.", DocRange.GetRange(context));
        }

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true) => actionSet.CurrentObject;
        public CodeType Type() => ThisType;
        public Scope ReturningScope() => ThisType?.GetObjectScope();
    }
}