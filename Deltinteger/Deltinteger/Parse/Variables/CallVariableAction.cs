using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class CallVariableAction : IExpression
    {
        public IIndexReferencer Calling { get; }
        public IExpression[] Index { get; }
        private readonly CodeType _anyType;

        public CallVariableAction(ITypeSupplier types, IIndexReferencer calling, IExpression[] index)
        {
            Calling = calling;
            Index = index ?? new IExpression[0];
            _anyType = types.Any();
        }

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            IWorkshopTree result = Calling.Parse(actionSet);

            for (int i = 0; i < Index.Length; i++)
                result = Element.ValueInArray(result, Index[i].Parse(actionSet));

            return result;
        }

        public Scope ReturningScope()
        {
            if (Calling.Type() == null) return Calling.ReturningScope();
            else return Type()?.GetObjectScope();
        }

        public CodeType Type()
        {
            var type = Calling.Type();
            for (int i = 0; i < Index.Length; i++)
            {
                if (type is ArrayType arrayType)
                    type = arrayType.ArrayOfType;
                else
                {
                    type = _anyType;
                    break;
                }
            }
            return type;
        }
    }
}