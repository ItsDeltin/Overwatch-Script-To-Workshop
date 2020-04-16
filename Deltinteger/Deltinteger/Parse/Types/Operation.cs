using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class TypeOperation
    {
        public static readonly Func<IWorkshopTree, IWorkshopTree, IWorkshopTree> Add = (l, r) => Element.Part<V_Add>(l, r);
        public static readonly Func<IWorkshopTree, IWorkshopTree, IWorkshopTree> Subtract = (l, r) => Element.Part<V_Subtract>(l, r);
        public static readonly Func<IWorkshopTree, IWorkshopTree, IWorkshopTree> Multiply = (l, r) => Element.Part<V_Multiply>(l, r);
        public static readonly Func<IWorkshopTree, IWorkshopTree, IWorkshopTree> Divide = (l, r) => Element.Part<V_Divide>(l, r);
        public static readonly Func<IWorkshopTree, IWorkshopTree, IWorkshopTree> Modulo = (l, r) => Element.Part<V_Modulo>(l, r);

        public TypeOperator Operator { get; }
        /// <summary>The righthand of the operator. May be null if there is no right operator.</summary>
        public CodeType Right { get; }
        /// <summary>The return type of the operation.</summary>
        public CodeType ReturnType { get; }
        private readonly Func<IWorkshopTree, IWorkshopTree, IWorkshopTree> Resolver;

        public TypeOperation(TypeOperator op, CodeType right, CodeType returnType, Func<IWorkshopTree, IWorkshopTree, IWorkshopTree> resolver)
        {
            Operator = op;
            Right = right ?? throw new ArgumentNullException(nameof(right));
            ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
            Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
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