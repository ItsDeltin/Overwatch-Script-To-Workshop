using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class TypeOperation
    {
        public TypeOperator Operator { get; }
        /// <summary>The righthand of the operator. May be null if there is no right operator.</summary>
        public CodeType Right { get; }
        /// <summary>The return type of the operation.</summary>
        public CodeType ReturnType { get; }
        private readonly Func<IWorkshopTree, IWorkshopTree, IWorkshopTree> Resolver;

        public TypeOperation(TypeOperator op, CodeType right, CodeType returnType = null, Func<IWorkshopTree, IWorkshopTree, IWorkshopTree> resolver = null)
        {
            Operator = op;
            Right = right ?? throw new ArgumentNullException(nameof(right));
            ReturnType = returnType ?? DefaultTypeFromOperator(op);
            Resolver = resolver ?? DefaultFromOperator(op);
        }

        public IWorkshopTree Resolve(IWorkshopTree left, IWorkshopTree right) => Resolver.Invoke(left, right);

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

        public static CodeType DefaultTypeFromOperator(TypeOperator op)
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
                    return BooleanType.Instance;

                case TypeOperator.Add:
                case TypeOperator.Divide:
                case TypeOperator.Modulo:
                case TypeOperator.Multiply:
                case TypeOperator.Pow:
                case TypeOperator.Subtract:
                    return NumberType.Instance;

                default: throw new NotImplementedException(op.ToString());
            }
        }

        public static Func<IWorkshopTree, IWorkshopTree, IWorkshopTree> DefaultFromOperator(TypeOperator op)
        {
            switch (op)
            {
                case TypeOperator.Add               : return (l, r) => Element.Add     (l, r);
                case TypeOperator.And               : return (l, r) => Element.And     (l, r);
                case TypeOperator.Divide            : return (l, r) => Element.Divide  (l, r);
                case TypeOperator.Modulo            : return (l, r) => Element.Modulo  (l, r);
                case TypeOperator.Multiply          : return (l, r) => Element.Multiply(l, r);
                case TypeOperator.Or                : return (l, r) => Element.Or      (l, r);
                case TypeOperator.Pow               : return (l, r) => Element.Pow     (l, r);
                case TypeOperator.Subtract          : return (l, r) => Element.Subtract(l, r);
                case TypeOperator.Equal             : return (l, r) => Element.Compare(l, Elements.Operator.Equal             , r);
                case TypeOperator.GreaterThan       : return (l, r) => Element.Compare(l, Elements.Operator.GreaterThan       , r);
                case TypeOperator.GreaterThanOrEqual: return (l, r) => Element.Compare(l, Elements.Operator.GreaterThanOrEqual, r);
                case TypeOperator.LessThan          : return (l, r) => Element.Compare(l, Elements.Operator.LessThan          , r);
                case TypeOperator.LessThanOrEqual   : return (l, r) => Element.Compare(l, Elements.Operator.LessThanOrEqual   , r);
                case TypeOperator.NotEqual          : return (l, r) => Element.Compare(l, Elements.Operator.NotEqual          , r);
                default: throw new NotImplementedException(op.ToString());
            }
        }
    }

    public enum TypeOperator
    {
        ///<summary>a ^ b</summary>
        Pow,
        ///<summary>a * b</summary>
        Multiply,
        ///<summary>a / b</summary>
        Divide,
        ///<summary>a % b</summary>
        Modulo,
        ///<summary>a + b</summary>
        Add,
        ///<summary>a - b</summary>
        Subtract,
        ///<summary>a < b</summary>
        LessThan,
        ///<summary>a <= b</summary>
        LessThanOrEqual,
        ///<summary>a == b</summary>
        Equal,
        ///<summary>a >= b</summary>
        GreaterThanOrEqual,
        ///<summary>a > b</summary>
        GreaterThan,
        ///<summary>a != b</summary>
        NotEqual,
        ///<summary>a (and) b</summary>
        And,
        ///<summary>a || b</summary>
        Or,
    }
}