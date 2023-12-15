using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;

#nullable enable

namespace Deltin.Deltinteger.Parse
{
    public interface IAssignmentOperation
    {
        AssignmentOperator Operator { get; }
        CodeType ValueType { get; }
        void Validate(ParseInfo parseInfo, DocRange range, IExpression value);
        void Resolve(AssignmentOperationInfo assignmentOperationInfo);
    }

    public class AssignmentOperation : IAssignmentOperation
    {
        public AssignmentOperator Operator { get; }
        public CodeType ValueType { get; }
        private readonly Action<ValidateOperationParams>? _validate;
        private readonly Action<AssignmentOperationInfo> _action;

        public AssignmentOperation(AssignmentOperator op, CodeType valueType)
        {
            Operator = op;
            ValueType = valueType;
            _action = DefaultActionFromOperator(op);
        }

        public AssignmentOperation(AssignmentOperator op, CodeType valueType, Action<AssignmentOperationInfo> action)
        {
            Operator = op;
            ValueType = valueType;
            _action = action;
        }

        public AssignmentOperation(AssignmentOperator op, CodeType valueType, Action<ValidateOperationParams> validate, Action<AssignmentOperationInfo> action)
        {
            Operator = op;
            ValueType = valueType;
            _validate = validate;
            _action = action;
        }

        public void Validate(ParseInfo parseInfo, DocRange range, IExpression value) => _validate?.Invoke(new(parseInfo, range, value));

        public void Resolve(AssignmentOperationInfo assignmentOperationInfo) => _action(assignmentOperationInfo);

        private static Action<AssignmentOperationInfo> DefaultActionFromOperator(AssignmentOperator op) => op switch
        {
            // op =
            AssignmentOperator.Equal => info => info.Set(),
            // op ^=
            AssignmentOperator.PowEqual => info => info.Modify(Operation.RaiseToPower),
            // op *=
            AssignmentOperator.MultiplyEqual => info => info.Modify(Operation.Multiply),
            // op /=
            AssignmentOperator.DivideEqual => info => info.Modify(Operation.Divide),
            // op %=
            AssignmentOperator.ModuloEqual => info => info.Modify(Operation.Modulo),
            // op +=
            AssignmentOperator.AddEqual => info => info.Modify(Operation.Add),
            // op -=
            AssignmentOperator.SubtractEqual => info => info.Modify(Operation.Subtract),
            _ => throw new NotImplementedException()
        };

        public static AssignmentOperator OperatorFromTokenType(TokenType tokenType) => tokenType switch
        {
            TokenType.Equal => AssignmentOperator.Equal,
            TokenType.HatEqual => AssignmentOperator.PowEqual,
            TokenType.MultiplyEqual => AssignmentOperator.MultiplyEqual,
            TokenType.DivideEqual => AssignmentOperator.DivideEqual,
            TokenType.ModuloEqual => AssignmentOperator.ModuloEqual,
            TokenType.AddEqual => AssignmentOperator.AddEqual,
            TokenType.SubtractEqual => AssignmentOperator.SubtractEqual,
            _ => throw new NotImplementedException()
        };

        public static AssignmentOperation[] GetNumericOperations(CodeType valueType) => new AssignmentOperation[] {
            new AssignmentOperation(AssignmentOperator.Equal, valueType),
            new AssignmentOperation(AssignmentOperator.AddEqual, valueType),
            new AssignmentOperation(AssignmentOperator.DivideEqual, valueType),
            new AssignmentOperation(AssignmentOperator.ModuloEqual, valueType),
            new AssignmentOperation(AssignmentOperator.MultiplyEqual, valueType),
            new AssignmentOperation(AssignmentOperator.PowEqual, valueType),
            new AssignmentOperation(AssignmentOperator.SubtractEqual, valueType)
        };
    }

    public record struct ValidateOperationParams(ParseInfo ParseInfo, DocRange Range, IExpression Value);

    public class AssignmentOperationInfo
    {
        public ActionSet ActionSet { get; }
        public IWorkshopTree Value { get; }
        private readonly VariableElements _elements;
        private readonly string _comment;

        public IGettable Variable => _elements.IndexReference;
        public Element Target => _elements.Target;

        public AssignmentOperationInfo(string comment, ActionSet actionSet, VariableElements variableInfo, IWorkshopTree value)
        {
            _comment = comment;
            ActionSet = actionSet;
            Value = value;
            _elements = variableInfo;
        }

        public void Set() => Variable.Set(ActionSet.SetNextComment(_comment), Value, Target);
        public void Modify(Operation operation) => Variable.Modify(ActionSet.SetNextComment(_comment), operation, Value, Target);
        public void ModifyWithValueCast(Operation operation, Func<IWorkshopTree, IWorkshopTree> caster) => Variable.Modify(ActionSet.SetNextComment(_comment), operation, caster(Value), Target);
    }
}