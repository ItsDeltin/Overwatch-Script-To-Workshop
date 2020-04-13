namespace Deltin.Deltinteger.Parse
{
    public class TypeOperation
    {
        public TypeOperator Operator { get; }
        /// <summary>The righthand of the operator. May be null if there is no right operator.</summary>
        public CodeType Right { get; }
        /// <summary>The return type of the operation.</summary>
        public CodeType ReturnType { get; }

        public TypeOperation(TypeOperator op, CodeType right, CodeType returnType)
        {
            Operator = op;
            Right = right;
        }
    }

    public enum TypeOperator
    {
        ///<summary>a + b</summary>
        Add,
        ///<summary>a - b</summary>
        Subtract,
        ///<summary>a * b</summary>
        Multiply,
        ///<summary>a / b</summary>
        Divide
    }
}