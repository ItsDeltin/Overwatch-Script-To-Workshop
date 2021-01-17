using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class CallVariableAction : ICallVariable, IExpression
    {
        public IVariableInstance Calling { get; }
        public IExpression[] Index { get; }
        private readonly CodeType _anyType;

        public CallVariableAction(ITypeSupplier types, IVariableInstance calling, IExpression[] index)
        {
            Calling = calling;
            Index = index ?? new IExpression[0];
            _anyType = types.Any();
        }

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            IWorkshopTree result = Calling.ToWorkshop(actionSet);

            for (int i = 0; i < Index.Length; i++)
                result = ValueInArrayToWorkshop.ValueInArray(result, Index[i].Parse(actionSet));

            return result;
        }

        public CodeType Type()
        {
            var type = Calling.CodeType;
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

        public void Accept() {}
    }
}