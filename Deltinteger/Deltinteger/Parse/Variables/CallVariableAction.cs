using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    class CallVariableAction : IExpression
    {
        public IIndexReferencer Calling { get; }
        public IExpression[] Index { get; }

        public CallVariableAction(IIndexReferencer calling, IExpression[] index)
        {
            Calling = calling;
            Index = index;
        }

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            IWorkshopTree result = Calling.Parse(actionSet);

            for (int i = 0; i < Index.Length; i++)
                result = Element.Part<V_ValueInArray>(result, Index[i].Parse(actionSet));

            return result;
        }

        public Element[] ParseIndex(ActionSet actionSet) => Array.ConvertAll(Index, index => (Element)index.Parse(actionSet));

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