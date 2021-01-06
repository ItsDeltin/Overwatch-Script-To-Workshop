using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class CallVariableAction : ICallVariable, IExpression
    {
        public IVariableInstance Calling { get; }
        public IExpression[] Index { get; }

        public CallVariableAction(IVariableInstance calling, IExpression[] index)
        {
            Calling = calling;
            Index = index ?? new IExpression[0];
        }

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            IWorkshopTree result = Calling.ToWorkshop(actionSet);

            for (int i = 0; i < Index.Length; i++)
                result = Element.ValueInArray(result, Index[i].Parse(actionSet));

            return result;
        }

        public CodeType Type()
        {
            var type = Calling.CodeType;
            for (int i = 0; i < Index.Length; i++)
            {
                if (type is ArrayType)
                    type = ((ArrayType)type).ArrayOfType;
                else
                {
                    type = null;
                    break;
                }
            }
            return type;
        }

        public void Accept() {}
    }
}