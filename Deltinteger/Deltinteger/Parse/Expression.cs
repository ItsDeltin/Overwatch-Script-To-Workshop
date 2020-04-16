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
        IWorkshopTree Parse(ActionSet actionSet);
    }

    public class NumberAction : IExpression
    {
        public double Value { get; }

        public NumberAction(ScriptFile script, DeltinScriptParser.NumberContext numberContext)
        {
            Value = double.Parse(numberContext.GetText());
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => NumberType.Instance;
        public IWorkshopTree Parse(ActionSet actionSet) => new V_Number(Value);
    }

    public class BoolAction : IExpression
    {
        public bool Value { get; }

        public BoolAction(ScriptFile script, bool value)
        {
            Value = value;
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => BooleanType.Instance;

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            if (Value) return new V_True();
            else return new V_False();
        }
    }

    public class NullAction : IExpression
    {
        public NullAction() {}
        public Scope ReturningScope() => null;
        public CodeType Type() => NullType.Instance;

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            return new V_Null();
        }
    }

    public class ValueInArrayAction : IExpression
    {
        public IExpression Expression { get; }
        public IExpression[] Index { get; }
        private ParseInfo parseInfo { get; }

        public ValueInArrayAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_array_indexContext exprContext)
        {
            Expression = parseInfo.GetExpression(scope, exprContext.array);
            this.parseInfo = parseInfo;

            if (exprContext.index == null)
                parseInfo.Script.Diagnostics.Error("Expected an expression.", DocRange.GetRange(exprContext.INDEX_START()));
            else
                Index = new IExpression[] { parseInfo.GetExpression(scope, exprContext.index) };
        }

        public ValueInArrayAction(ParseInfo parseInfo, IExpression expression, IExpression[] index)
        {
            Expression = expression;
            Index = index;
            this.parseInfo = parseInfo;
        }

        public Scope ReturningScope() => Type()?.GetObjectScope() ?? parseInfo.TranslateInfo.PlayerVariableScope;
        public CodeType Type() => (Expression.Type() as ArrayType)?.ArrayOfType;
        public IWorkshopTree Parse(ActionSet actionSet)
        {
            IWorkshopTree result = Expression.Parse(actionSet);

            foreach(var index in Index)
                result = Element.Part<V_ValueInArray>(result, index.Parse(actionSet));

            return result;
        }
    }

    public class CreateArrayAction : IExpression
    {
        public IExpression[] Values { get; }

        public CreateArrayAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.CreatearrayContext createArrayContext)
        {
            Values = new IExpression[createArrayContext.expr().Length];
            for (int i = 0; i < Values.Length; i++)
                Values[i] = parseInfo.GetExpression(scope, createArrayContext.expr(i));
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;

        public IWorkshopTree Parse(ActionSet actionSet)
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
            // Get the expression. Syntax error if there is none.
            if (typeConvert.expr() == null)
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(typeConvert.GREATER_THAN()));
            else
                Expression = parseInfo.GetExpression(scope, typeConvert.expr());

            // Get the type. Syntax error if there is none.
            if (typeConvert.code_type() == null)
                parseInfo.Script.Diagnostics.Error("Expected type name.", DocRange.GetRange(typeConvert.LESS_THAN()));
            else
                ConvertingTo = CodeType.GetCodeTypeFromContext(parseInfo, typeConvert.code_type());
        }

        public Scope ReturningScope() => ConvertingTo?.GetObjectScope();
        public CodeType Type() => ConvertingTo;
        public IWorkshopTree Parse(ActionSet actionSet) => Expression.Parse(actionSet);
    }

    public class NotAction : IExpression
    {
        public IExpression Expression { get; }

        public NotAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.ExprContext exprContext)
        {
            Expression = parseInfo.GetExpression(scope, exprContext);
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;
        public IWorkshopTree Parse(ActionSet actionSet) => Element.Part<V_Not>(Expression.Parse(actionSet));
    }
    
    public class InverseAction : IExpression
    {
        public IExpression Expression { get; }

        public InverseAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.ExprContext exprContext)
        {
            Expression = parseInfo.GetExpression(scope, exprContext);
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;
        public IWorkshopTree Parse(ActionSet actionSet) => Element.Part<V_Multiply>(Expression.Parse(actionSet), new V_Number(-1));
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
            Condition = parseInfo.GetExpression(scope, ternaryContext.condition);

            if (ternaryContext.consequent == null)
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(ternaryContext.TERNARY()));
            else
                Consequent = parseInfo.GetExpression(scope, ternaryContext.consequent);
            
            if (ternaryContext.alternative == null)
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(ternaryContext.TERNARY_ELSE()));
            else
                Alternative = parseInfo.GetExpression(scope, ternaryContext.alternative);
        }

        public Scope ReturningScope() => Type()?.GetObjectScope() ?? parseInfo.TranslateInfo.PlayerVariableScope;
        public CodeType Type()
        {
            // Consequent or Alternative can equal null on GetExpression failure.
            if (Consequent != null && Alternative != null && Consequent.Type() == Alternative.Type()) return Consequent.Type();
            return null;
        }
        public IWorkshopTree Parse(ActionSet actionSet) => Element.TernaryConditional(Condition.Parse(actionSet), Consequent.Parse(actionSet), Alternative.Parse(actionSet));
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
        public IWorkshopTree Parse(ActionSet actionSet) => null;
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

        public IWorkshopTree Parse(ActionSet actionSet) => actionSet.This;
        public CodeType Type() => ThisType;
        public Scope ReturningScope() => ThisType?.GetObjectScope();
    }

    public class BaseAction : IExpression
    {
        readonly CodeType baseType;

        public BaseAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_baseContext context)
        {
            CodeType thisType = scope.GetThis();

            // Syntax error if the 'base' keyword is used outside of classes.
            if (thisType == null)
                parseInfo.Script.Diagnostics.Error("Keyword 'base' cannot be used here.", DocRange.GetRange(context));
            
            // Syntax error if the current class does not extend anything.
            else if (thisType.Extends == null)
                parseInfo.Script.Diagnostics.Error("The current type does not extend a class.", DocRange.GetRange(context));
            
            else baseType = thisType.Extends;
        }

        public IWorkshopTree Parse(ActionSet actionSet) => null;
        public Scope ReturningScope() => baseType?.GetObjectScope();
        public CodeType Type() => baseType;
    }

    public class IsAction : IExpression
    {
        readonly IExpression expression;
        readonly ClassType checkingIfType;

        public IsAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_isContext isContext)
        {
            // Get the expression.
            expression = parseInfo.GetExpression(scope, isContext.expr());

            // Get the type.
            if (isContext.type == null)
                parseInfo.Script.Diagnostics.Error("Expected type name.", DocRange.GetRange(isContext.IS()));
            else
            {
                CodeType type = parseInfo.TranslateInfo.Types.GetCodeType(isContext.type.Text, parseInfo.Script.Diagnostics, DocRange.GetRange(isContext.type));

                // Make sure the received type is a class.
                if (type != null && type is ClassType == false)
                    parseInfo.Script.Diagnostics.Error("Expected a class type.", DocRange.GetRange(isContext.type));
                else
                    checkingIfType = (ClassType)type;
            }
        }

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            ClassData classData = actionSet.Translate.DeltinScript.GetComponent<ClassData>();

            // Parse the expression.
            IWorkshopTree expressionResult = expression.Parse(actionSet);

            // Get the class identifier of the input expression.
            IWorkshopTree classIdentifier = Element.Part<V_ValueInArray>(classData.ClassIndexes.GetVariable(), expressionResult);

            // Check if the expression's class identifier and the type are equal.
            return new V_Compare(classIdentifier, Operators.Equal, new V_Number(checkingIfType.Identifier));
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;
    }
}