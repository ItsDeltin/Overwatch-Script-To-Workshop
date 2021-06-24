using System.Linq;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public class TypeOperatorInfo
    {
        public bool DefaultAssignment { get; set; } = true;
        private readonly List<ITypeOperation> _typeOperations = new List<ITypeOperation>();
        private readonly List<IUnaryTypeOperation> _unaryTypeOperations = new List<IUnaryTypeOperation>();
        private readonly List<IAssignmentOperation> _assignmentOperations = new List<IAssignmentOperation>();
        private readonly CodeType _type;

        public TypeOperatorInfo(CodeType type)
        {
            _type = type;
        }

        public void AddTypeOperation(params ITypeOperation[] operations) => _typeOperations.AddRange(operations);
        public void AddTypeOperation(params IUnaryTypeOperation[] operations) => _unaryTypeOperations.AddRange(operations);
        public void AddTypeOperation(params IAssignmentOperation[] operations) => _assignmentOperations.AddRange(operations);

        public void AddAssignmentOperator()
        {
            // = assignment already exists.
            if (IsAssignmentOperator(AssignmentOperator.Equal))
                return;
            
            AddTypeOperation(new AssignmentOperation(AssignmentOperator.Equal, _type));
        }

        public bool IsBinaryOperator(TypeOperator op) => _typeOperations.Any(binaryOp => op == binaryOp.Operator);
        public bool IsUnaryOperator(UnaryTypeOperator op) => _unaryTypeOperations.Any(unaryOp => op == unaryOp.Operator);
        public bool IsAssignmentOperator(AssignmentOperator op) => _assignmentOperations.Any(assignmentOp => op == assignmentOp.Operator);

        /// <summary>Gets a binary operation.</summary>
        /// <param name="op">The operation's operator type.</param>
        /// <param name="right">The right object's type.</param>
        /// <returns>A TypeOperation if the operation is found. Null if it is not found.</returns>
        public virtual ITypeOperation GetOperation(TypeOperator op, CodeType right)
        {
            CodeType current = _type;
            while (current != null)
            {
                if (current.Operations != null)
                    foreach (ITypeOperation operation in current.Operations._typeOperations)
                        if (operation.Operator == op && right != null && right.Implements(operation.Right))
                            return operation;
                
                current = current.Extends;
            }
            return null;
        }

        /// <summary>Gets an unary operation.</summary>
        /// <param name="op">The operation's operator type.</param>
        /// <returns>A TypeOperation if the operation is found. Null if it is not found.</returns>
        public virtual IUnaryTypeOperation GetOperation(UnaryTypeOperator op)
        {
            CodeType current = _type;
            while (current != null)
            {
                if (current.Operations != null)
                    foreach (IUnaryTypeOperation operation in current.Operations._unaryTypeOperations)
                        if (operation.Operator == op)
                            return operation;
                
                current = current.Extends;
            }
            return null;
        }

        /// <summary>Gets an assignment operation.</summary>
        public virtual IAssignmentOperation GetOperation(AssignmentOperator op, CodeType value)
        {
            CodeType current = _type;
            while (current != null)
            {
                if (current.Operations != null)
                    foreach (IAssignmentOperation operation in current.Operations._assignmentOperations)
                        if (operation.Operator == op && value != null && value.Implements(operation.ValueType))
                            return operation;
                
                current = current.Extends;
            }
            return null;
        }
    }
}