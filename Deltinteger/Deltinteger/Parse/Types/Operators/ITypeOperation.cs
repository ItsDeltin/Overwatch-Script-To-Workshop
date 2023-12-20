using System;
using System.Diagnostics.CodeAnalysis;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Elements;

#nullable enable

namespace Deltin.Deltinteger.Parse
{
    public interface ITypeOperation
    {
        TypeOperator Operator { get; }
        CodeType Right { get; }
        CodeType ReturnType { get; }
        void Validate(ParseInfo parseInfo, DocRange range, IExpression left, IExpression right);
        IWorkshopTree Resolve(ActionSet actionSet, IExpression left, IExpression right);
    }

    public class TypeOperation : ITypeOperation
    {
        public TypeOperator Operator { get; }
        /// <summary>The righthand of the operator. May be null if there is no right operator.</summary>
        public CodeType Right { get; }
        /// <summary>The return type of the operation.</summary>
        public CodeType ReturnType { get; }
        private readonly Func<CompileParams, IWorkshopTree> Resolver;
        private readonly Action<ExpressionOperationValidationParams>? Validator;

        public TypeOperation(ITypeSupplier supplier, TypeOperator op, CodeType right)
        {
            Operator = op;
            Right = right ?? throw new ArgumentNullException(nameof(right));
            ReturnType = DefaultTypeFromOperator(op, supplier);
            Resolver = ConvertSimpleFunctionType(DefaultFromOperator(op));
        }

        public TypeOperation(TypeOperator op, CodeType right, CodeType returnType)
        {
            Operator = op;
            Right = right ?? throw new ArgumentNullException(nameof(right));
            ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
            Resolver = ConvertSimpleFunctionType(DefaultFromOperator(op));
        }

        public TypeOperation(ITypeSupplier supplier, TypeOperator op, CodeType right, Func<IWorkshopTree, IWorkshopTree, IWorkshopTree> resolver)
        {
            Operator = op;
            Right = right ?? throw new ArgumentNullException(nameof(right));
            ReturnType = DefaultTypeFromOperator(op, supplier);
            Resolver = ConvertSimpleFunctionType(resolver) ?? throw new ArgumentNullException(nameof(resolver));
        }

        public TypeOperation(TypeOperator op, CodeType right, CodeType returnType, Func<IWorkshopTree, IWorkshopTree, IWorkshopTree> resolver)
        {
            Operator = op;
            Right = right ?? throw new ArgumentNullException(nameof(right));
            ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
            Resolver = ConvertSimpleFunctionType(resolver) ?? throw new ArgumentNullException(nameof(resolver));
        }

        public TypeOperation(
            TypeOperator op,
            CodeType right,
            CodeType returnType,
            Action<ExpressionOperationValidationParams> validator,
            Func<CompileParams, IWorkshopTree> resolver)
        {
            Operator = op;
            Right = right ?? throw new ArgumentNullException(nameof(right));
            ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
            Validator = validator ?? throw new ArgumentNullException(nameof(validator));
            Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public void Validate(ParseInfo parseInfo, DocRange range, IExpression left, IExpression right) => Validator?.Invoke(new(parseInfo, range, left, right));

        public IWorkshopTree Resolve(ActionSet actionSet, IExpression left, IExpression right) => Resolver.Invoke(new(
            actionSet, left.Parse(actionSet), right.Parse(actionSet)));

        public static TypeOperator TypeOperatorFromString(string str)
        {
            switch (str)
            {
                case "+": return TypeOperator.Add;
                case "-": return TypeOperator.Subtract;
                case "*": return TypeOperator.Multiply;
                case "/": return TypeOperator.Divide;
                case "^": return TypeOperator.Pow;
                case "%": return TypeOperator.Modulo;
                case "<": return TypeOperator.LessThan;
                case "<=": return TypeOperator.LessThanOrEqual;
                case "==": return TypeOperator.Equal;
                case ">=": return TypeOperator.GreaterThanOrEqual;
                case ">": return TypeOperator.GreaterThan;
                case "!=": return TypeOperator.NotEqual;
                case "&&": return TypeOperator.And;
                case "||": return TypeOperator.Or;
                default: throw new NotImplementedException();
            }
        }

        public static CodeType DefaultTypeFromOperator(TypeOperator op, ITypeSupplier supplier)
        {
            switch (op)
            {
                case TypeOperator.And:
                case TypeOperator.Or:
                case TypeOperator.NotEqual:
                case TypeOperator.Equal:
                case TypeOperator.GreaterThan:
                case TypeOperator.GreaterThanOrEqual:
                case TypeOperator.LessThan:
                case TypeOperator.LessThanOrEqual:
                    return supplier.Boolean();

                case TypeOperator.Add:
                case TypeOperator.Divide:
                case TypeOperator.Modulo:
                case TypeOperator.Multiply:
                case TypeOperator.Pow:
                case TypeOperator.Subtract:
                    return supplier.Number();

                default: throw new NotImplementedException(op.ToString());
            }
        }

        public static Func<IWorkshopTree, IWorkshopTree, IWorkshopTree> DefaultFromOperator(TypeOperator op)
        {
            switch (op)
            {
                case TypeOperator.Add: return (l, r) => Element.Add(l, r);
                case TypeOperator.And: return (l, r) => Element.And(l, r);
                case TypeOperator.Divide: return (l, r) => Element.Divide(l, r);
                case TypeOperator.Modulo: return (l, r) => Element.Modulo(l, r);
                case TypeOperator.Multiply: return (l, r) => Element.Multiply(l, r);
                case TypeOperator.Or: return (l, r) => Element.Or(l, r);
                case TypeOperator.Pow: return (l, r) => Element.Pow(l, r);
                case TypeOperator.Subtract: return (l, r) => Element.Subtract(l, r);
                case TypeOperator.Equal: return (l, r) => Element.Compare(l, Elements.Operator.Equal, r);
                case TypeOperator.GreaterThan: return (l, r) => Element.Compare(l, Elements.Operator.GreaterThan, r);
                case TypeOperator.GreaterThanOrEqual: return (l, r) => Element.Compare(l, Elements.Operator.GreaterThanOrEqual, r);
                case TypeOperator.LessThan: return (l, r) => Element.Compare(l, Elements.Operator.LessThan, r);
                case TypeOperator.LessThanOrEqual: return (l, r) => Element.Compare(l, Elements.Operator.LessThanOrEqual, r);
                case TypeOperator.NotEqual: return (l, r) => Element.Compare(l, Elements.Operator.NotEqual, r);
                default: throw new NotImplementedException(op.ToString());
            }
        }

        /// <summary>Converts '(l, r) => value' func to '(params) => value' func.
        /// The latter is nicer to type but the former has access to the action set.</summary>
        [return: NotNullIfNotNull("simple")]
        private static Func<CompileParams, IWorkshopTree>? ConvertSimpleFunctionType(
            Func<IWorkshopTree, IWorkshopTree, IWorkshopTree>? simple
        ) => simple == null ? null : (param) => simple(param.Left, param.Right);

        public record struct CompileParams(ActionSet ActionSet, IWorkshopTree Left, IWorkshopTree Right);
    }

    public record struct ExpressionOperationValidationParams(ParseInfo ParseInfo, DocRange Range, IExpression Left, IExpression Right);
}