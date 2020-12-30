using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public class TypeOperatorInfo
    {
        public List<ITypeOperation> TypeOperations { get; } = new List<ITypeOperation>();
        public List<IUnaryTypeOperation> UnaryTypeOperations { get; } = new List<IUnaryTypeOperation>();
        public bool DefaultEquality { get; set; } = true;
        public bool DefaultComparison { get; set; } = true;
        public bool DefaultBooleanLogic { get; set; } = true;
        public bool DefaultMath { get; set; } = true;
        private readonly CodeType _type;

        public TypeOperatorInfo(CodeType type)
        {
            _type = type;
        }

        public void AddTypeOperation(params ITypeOperation[] operations) => TypeOperations.AddRange(operations);
        public void AddTypeOperation(params IUnaryTypeOperation[] operations) => UnaryTypeOperations.AddRange(operations);

        /// <summary>Gets an operation.</summary>
        /// <param name="op">The operation's operator type.</param>
        /// <param name="right">The right object's type.</param>
        /// <returns>A TypeOperation if the operation is found. Null if it is not found.</returns>
        public ITypeOperation GetOperation(TypeOperator op, CodeType right)
        {
            CodeType current = _type;
            while (current != null)
            {
                if (current.Operations != null)
                    foreach (ITypeOperation operation in current.Operations.TypeOperations)
                        if (operation.Operator == op && right != null && right.Implements(operation.Right))
                            return operation;
                
                current = current.Extends;
            }
            return null;
        }

        /// <summary>Gets an operation.</summary>
        /// <param name="op">The operation's operator type.</param>
        /// <returns>A TypeOperation if the operation is found. Null if it is not found.</returns>
        public IUnaryTypeOperation GetOperation(UnaryTypeOperator op)
        {
            CodeType current = _type;
            while (current != null)
            {
                if (current.Operations != null)
                    foreach (IUnaryTypeOperation operation in current.Operations.UnaryTypeOperations)
                        if (operation.Operator == op)
                            return operation;
                
                current = current.Extends;
            }
            return null;
        }
    }
}