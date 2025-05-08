using System;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public interface IExpression
    {
        Scope ReturningScope() => Type()?.GetObjectScope();
        CodeType Type();
        IWorkshopTree Parse(ActionSet actionSet);
        bool IsStatement() => false;
    }

    public class NumberAction : IExpression
    {
        public double Value { get; }
        private readonly CodeType _type;

        public NumberAction(ParseInfo parseInfo, NumberExpression numberContext)
        {
            Value = numberContext.Value;
            _type = parseInfo.TranslateInfo.Types.Number();
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => _type;
        public IWorkshopTree Parse(ActionSet actionSet) => Element.Num(Value);
    }

    public class BoolAction : IExpression
    {
        public bool Value { get; }
        private readonly CodeType _type;

        public BoolAction(ParseInfo parseInfo, bool value)
        {
            Value = value;
            _type = parseInfo.TranslateInfo.Types.Boolean();
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => _type;

        public IWorkshopTree Parse(ActionSet actionSet) => Value ? Element.True() : Element.False();
    }

    public class NullAction : IExpression
    {
        private readonly CodeType _type;

        public NullAction(ParseInfo parseInfo) => _type = parseInfo.TranslateInfo.Types.Any();
        public Scope ReturningScope() => null;
        public CodeType Type() => _type;
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
            var expressionType = Expression.Type();
            if (CodeTypeHelpers.IsParallel(expressionType) && !CodeTypeHelpers.IsArray(expressionType))
            {
                parseInfo.Error("This struct cannot be indexed", context.Array.Range);
            }

            var indexExpression = parseInfo.ClearContextual().GetExpression(scope, context.Index);
            if (CodeTypeHelpers.IsParallel(indexExpression.Type()))
            {
                parseInfo.Script.Diagnostics.Error("Structs cannot be used as an indexer", context.Index.Range);
            }

            Index = new IExpression[] { indexExpression };
            this.parseInfo = parseInfo;
        }

        public Scope ReturningScope() => Type()?.GetObjectScope() ?? parseInfo.TranslateInfo.PlayerVariableScope;
        public CodeType Type() => (Expression.Type() as ArrayType)?.ArrayOfType ?? parseInfo.Types.Any();
        public IWorkshopTree Parse(ActionSet actionSet)
        {
            IWorkshopTree result = Expression.Parse(actionSet);

            foreach (var index in Index)
                result = Element.ValueInArray(result, index.Parse(actionSet));

            return result;
        }
    }

    public class CreateArrayAction : IExpression
    {
        public IExpression[] Values { get; }
        private readonly ArrayType _type;
        private readonly bool _isStructArray; // Determines if this is definitely a struct array. Will be false for empty struct arrays.

        public CreateArrayAction(ParseInfo parseInfo, Scope scope, CreateArray createArrayContext)
        {
            Values = new IExpression[createArrayContext.Values.Count];
            for (int i = 0; i < Values.Length; i++)
            {
                var expectingType = parseInfo.ExpectingType;
                if (expectingType is ArrayType expectingArray)
                    expectingType = expectingArray.ArrayOfType;

                Values[i] = parseInfo.SetExpectType(expectingType).GetExpression(scope, createArrayContext.Values[i]);
            }

            if (Values.Length == 0)
                _type = new ArrayType(parseInfo.TranslateInfo.Types, parseInfo.TranslateInfo.Types.Unknown());
            else
            {
                // The type of the array is the type of the first value.
                var sourceType = Values[0].Type();
                _type = new ArrayType(parseInfo.TranslateInfo.Types, sourceType);

                // Struct array
                _isStructArray = sourceType.Attributes.IsStruct;

                // If this is a struct array, the types are strict.
                if (_isStructArray)
                    for (int i = 0; i < Values.Length; i++)
                        SemanticsHelper.ExpectValueType(parseInfo, Values[i], sourceType, createArrayContext.Values[i].Range);
            }
        }

        public CodeType Type() => _type;

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            IWorkshopTree[] asWorkshop = new IWorkshopTree[Values.Length];
            for (int i = 0; i < asWorkshop.Length; i++)
                asWorkshop[i] = Values[i].Parse(actionSet);

            bool isStruct = _type.ArrayOfType.GetRealType(actionSet.ThisTypeLinker).Attributes.IsStruct;

            // Struct array
            if (isStruct || asWorkshop.Any(value => value is IStructValue))
                return new StructArray(Array.ConvertAll(asWorkshop, item => (IStructValue)item));

            // Normal array
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
        private readonly IUnaryTypeOperation _operation;
        private readonly CodeType _type;

        public UnaryOperatorAction(ParseInfo parseInfo, Scope scope, UnaryOperatorExpression expression)
        {
            Value = parseInfo.GetExpression(scope, expression.Value);

            string op = expression.Operator.Text;
            _operation = Value?.Type()?.Operations.GetOperation(UnaryTypeOperation.OperatorFromString(op)) ?? GetDefaultOperation(op, parseInfo.TranslateInfo.Types);
            _type = _operation.ReturnType ?? parseInfo.TranslateInfo.Types.Unknown();
        }

        public IWorkshopTree Parse(ActionSet actionSet) => _operation.Resolve(actionSet, Value);

        private UnaryTypeOperation GetDefaultOperation(string op, ITypeSupplier supplier)
        {
            if (Value.Type().IsConstant())
                return null;

            switch (op)
            {
                case "!": return new UnaryTypeOperation(UnaryTypeOperation.OperatorFromString(op), supplier.Boolean(), v => !(Element)v);
                case "-": return new UnaryTypeOperation(UnaryTypeOperation.OperatorFromString(op), supplier.Number(), v => (Element)v * -1);
                default: throw new NotImplementedException();
            }
        }

        public CodeType Type() => _type;
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

            if (Consequent.Type() != null && Consequent.Type().IsConstant())
                parseInfo.Script.Diagnostics.Error($"Cannot use constant types in a ternary expression.", ternaryContext.Consequent.Range);
            if (Alternative.Type() != null && Alternative.Type().IsConstant())
                parseInfo.Script.Diagnostics.Error($"Cannot use constant types in a ternary expression.", ternaryContext.Alternative.Range);
        }

        public Scope ReturningScope() => Type()?.GetObjectScope() ?? parseInfo.TranslateInfo.PlayerVariableScope;
        public CodeType Type()
        {
            var consequentType = Consequent.Type();
            var alternativeType = Alternative.Type();

            // If the types are the same, the ternary type is that type.
            if (consequentType.Is(alternativeType))
                return consequentType;
            // Otherwise, if the types are compatible, create a union with those types.
            if (consequentType.CompatibleWith(alternativeType))
                return new PipeType(consequentType, alternativeType);

            // Otherwise, the type is Any.
            return parseInfo.Types.Any();
        }
        public IWorkshopTree Parse(ActionSet actionSet)
        {
            var condition = Condition.Parse(actionSet);
            var consequent = Consequent.Parse(actionSet);
            var alternative = Alternative.Parse(actionSet);

            if (consequent is IStructValue consequentStruct)
                return new TernaryConditionalStruct(condition, consequentStruct, (IStructValue)alternative);

            return Element.TernaryConditional(condition, consequent, alternative);
        }

        class TernaryConditionalStruct : IStructValue
        {
            readonly IWorkshopTree _condition;
            readonly IStructValue _consequent;
            readonly IStructValue _alternative;

            public TernaryConditionalStruct(IWorkshopTree condition, IStructValue consequent, IStructValue alternative)
            {
                _condition = condition;
                _consequent = consequent;
                _alternative = alternative;
            }

            public IWorkshopTree GetValue(string variableName)
            {
                // Get the consequent and alternative.
                var consequent = _consequent.GetValue(variableName);
                var alternative = _alternative.GetValue(variableName);

                // Check if we need to do a ternary subsection.
                if (consequent is IStructValue consequentStruct)
                    return new TernaryConditionalStruct(_condition, consequentStruct, (IStructValue)alternative);

                // Otherwise, create the ternary normally.
                return Element.TernaryConditional(_condition, consequent, alternative);
            }

            public IWorkshopTree[] GetAllValues() => _consequent.GetAllValues();
            public IGettable GetGettable(string variableName) => new WorkshopElementReference(GetValue(variableName));
            public IWorkshopTree GetArbritraryValue() => _consequent;
            public string[] GetNames() => _consequent.GetNames();
        }
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
        readonly IDefinedTypeInitializer _typeInitializer;
        readonly CodeType _type;

        public ThisAction(ParseInfo parseInfo, Scope scope, ThisExpression context)
        {
            _typeInitializer = parseInfo.TypeInitializer;
            if (_typeInitializer == null)
            {
                parseInfo.Script.Diagnostics.Error("Keyword 'this' cannot be used here.", context.Range);
                _type = parseInfo.Types.Unknown();
            }
            else
                _type = _typeInitializer.WorkingInstance;
        }

        public IWorkshopTree Parse(ActionSet actionSet) => actionSet.This;
        public CodeType Type() => _type;
        public Scope ReturningScope() => _typeInitializer?.GetObjectBasedScope();
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