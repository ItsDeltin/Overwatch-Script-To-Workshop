using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public interface IExpression
    {
        Scope ReturningScope();
        CodeType Type();
        IWorkshopTree Parse(ActionSet actionSet);
        bool IsStatement() => false;
    }

    public class NumberAction : IExpression
    {
        public double Value { get; }

        public NumberAction(ScriptFile script, NumberExpression numberContext)
        {
            Value = numberContext.Value;
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => NumberType.Instance;
        public IWorkshopTree Parse(ActionSet actionSet) => Element.Num(Value);
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

        public IWorkshopTree Parse(ActionSet actionSet) => Value ? Element.True() : Element.False();
    }

    public class NullAction : IExpression
    {
        public NullAction() {}
        public Scope ReturningScope() => null;
        public CodeType Type() => NullType.Instance;
        public IWorkshopTree Parse(ActionSet actionSet) => Element.Null();
    }

    public class ValueInArrayAction : IExpression
    {
        public IExpression Expression { get; }
        public IExpression[] Index { get; }
        private ParseInfo parseInfo { get; }

        public ValueInArrayAction(ParseInfo parseInfo, Scope scope, ValueInArray context)
        {
            Expression = parseInfo.GetExpression(scope, context.Array);
            Index = new IExpression[] { parseInfo.GetExpression(scope, context.Index) };
            this.parseInfo = parseInfo;
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
                result = Element.ValueInArray(result, index.Parse(actionSet));

            return result;
        }
    }

    public class CreateArrayAction : IExpression
    {
        public IExpression[] Values { get; }

        public CreateArrayAction(ParseInfo parseInfo, Scope scope, CreateArray createArrayContext)
        {
            Values = new IExpression[createArrayContext.Values.Count];
            for (int i = 0; i < Values.Length; i++)
                Values[i] = parseInfo.GetExpression(scope, createArrayContext.Values[i]);
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

        public TypeConvertAction(ParseInfo parseInfo, Scope scope, TypeCast typeConvert)
        {
            // Get the expression. Syntax error if there is none.
            Expression = parseInfo.GetExpression(scope, typeConvert.Expression);

            // Get the type. Syntax error if there is none.
            ConvertingTo = TypeFromContext.GetCodeTypeFromContext(parseInfo, scope, typeConvert.Type);
        }

        public Scope ReturningScope() => ConvertingTo?.GetObjectScope();
        public CodeType Type() => ConvertingTo;
        public IWorkshopTree Parse(ActionSet actionSet) => Expression.Parse(actionSet);
    }

    public class UnaryOperatorAction : IExpression
    {
        public IExpression Value { get; }
        public OperatorInfo Operator { get; }

        public UnaryOperatorAction(ParseInfo parseInfo, Scope scope, UnaryOperatorExpression expression)
        {
            Value = parseInfo.GetExpression(scope, expression.Value);
            Operator = expression.Operator;
        }

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            // OperatorInfo (an operator instance) ->
            // CompilerOperator (the actual operator data) ->
            // Operator's actual string identifier
            switch (Operator.Operator.Operator)
            {
                // Not
                case "!":
                    return Element.Not(Value.Parse(actionSet));
                // Inverse
                case "-":
                    return Element.Multiply(Value.Parse(actionSet), Element.Num(-1));
                // Unimplemented unary operator.
                default:
                    throw new NotImplementedException(Operator.Operator.Operator);
            }
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;
    }

    public class TernaryConditionalAction : IExpression
    {
        public IExpression Condition { get; }
        public IExpression Consequent { get; }
        public IExpression Alternative { get; }
        private ParseInfo parseInfo { get; }

        public TernaryConditionalAction(ParseInfo parseInfo, Scope scope, TernaryExpression ternaryContext)
        {
            this.parseInfo = parseInfo;
            
            Condition = parseInfo.GetExpression(scope, ternaryContext.Condition);
            Consequent = parseInfo.GetExpression(scope, ternaryContext.Consequent);
            Alternative = parseInfo.GetExpression(scope, ternaryContext.Alternative);
        }

        public Scope ReturningScope() => Type()?.GetObjectScope() ?? parseInfo.TranslateInfo.PlayerVariableScope;
        public CodeType Type()
        {
            if (Consequent.Type() == Alternative.Type()) return Consequent.Type();
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

        public ThisAction(ParseInfo parseInfo, Scope scope, ThisExpression context)
        {
            ThisType = scope.GetThis();
            if (ThisType == null)
                parseInfo.Script.Diagnostics.Error("Keyword 'this' cannot be used here.", context.Range);
        }

        public IWorkshopTree Parse(ActionSet actionSet) => actionSet.This;
        public CodeType Type() => ThisType;
        public Scope ReturningScope() => ThisType?.GetObjectScope();
    }

    /*
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
            IWorkshopTree classIdentifier = classData.ClassIndexes.Get()[expressionResult];

            // Check if the expression's class identifier and the type are equal.
            return Element.Compare(classIdentifier, Operator.Equal, new NumberElement(checkingIfType.Identifier));
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;
    }
    */
}