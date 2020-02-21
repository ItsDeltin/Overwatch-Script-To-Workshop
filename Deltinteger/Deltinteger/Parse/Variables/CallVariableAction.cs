using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class CallVariableAction : IExpression
    {
        public IIndexReferencer Calling { get; }
        public IExpression[] Index { get; }
        private readonly DeltinScript translateInfo;

        public CallVariableAction(DeltinScript translateInfo, IIndexReferencer calling, IExpression[] index)
        {
            Calling = calling;
            Index = index;
            this.translateInfo = translateInfo;
        }

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            IWorkshopTree result = Calling.Parse(actionSet);

            for (int i = 0; i < Index.Length; i++)
                result = Element.Part<V_ValueInArray>(result, Index[i].Parse(actionSet));

            return result;
        }

        public Scope ReturningScope()
        {
            return Type()?.GetObjectScope(translateInfo);
        }

        public CodeType Type()
        {
            var type = Calling.Type();
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
    }
}