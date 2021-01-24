using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class TypeOperatorInfo
    {
        private readonly List<ITypeOperation> _typeOperations = new List<ITypeOperation>();
        private readonly List<IUnaryTypeOperation> _unaryTypeOperations = new List<IUnaryTypeOperation>();
        private readonly List<IAssignmentOperation> _assignmentOperations = new List<IAssignmentOperation>();
        public bool DefaultAssignment { get; set; } = true;
        private readonly CodeType _type;

        public TypeOperatorInfo(CodeType type)
        {
            _type = type;
        }

        public void AddTypeOperation(params ITypeOperation[] operations) => _typeOperations.AddRange(operations);
        public void AddTypeOperation(params IUnaryTypeOperation[] operations) => _unaryTypeOperations.AddRange(operations);
        public void AddTypeOperation(params IAssignmentOperation[] operations) => _assignmentOperations.AddRange(operations);

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