using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deltin.Deltinteger.Elements;

// !
// TODO: remove
// !

namespace Deltin.Deltinteger.Parse
{
    public class ForeachBuilder
    {
        private ActionSet ActionSet { get; }
        private IndexReference IndexStore { get; }
        private Element Condition { get; }
        public IWorkshopTree Array { get; }
        public Element Index { get; }
        public Element IndexValue { get; }

        public ForeachBuilder(ActionSet actionSet, IWorkshopTree array)
        {
            ActionSet = actionSet;
            IndexStore = actionSet.VarCollection.Assign("foreachIndex,", actionSet.IsGlobal, true);
            Array = array;
            Condition = new V_Compare(IndexStore.GetVariable(), Operators.LessThan, Element.Part<V_CountOf>(Array));
            Index = (Element)IndexStore.GetVariable();
            IndexValue = Element.Part<V_ValueInArray>(Array, IndexStore.GetVariable());

            actionSet.AddAction(IndexStore.SetVariable(0));
            actionSet.AddAction(Element.Part<A_While>(Condition));
        }

        public void Finish()
        {
            ActionSet.AddAction(IndexStore.ModifyVariable(Operation.Add, 1));
            ActionSet.AddAction(new A_End());
        }
    }
}