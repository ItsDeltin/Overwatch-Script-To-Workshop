using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class WhileBuilder
    {
        protected ActionSet ActionSet { get; }
        public bool WasSetup { get; private set; }
        // Start of the loop, using End to mark the end that the continue skip will jump to.
        private SkipEndMarker Start { get; set; }
        public IWorkshopTree Condition { get; protected set; }

        public WhileBuilder(ActionSet actionSet, IWorkshopTree condition)
        {
            ActionSet = actionSet;
            Condition = condition;
        }
        protected WhileBuilder(ActionSet actionSet)
        {
            ActionSet = actionSet;
        }

        public void Setup()
        {
            if (WasSetup) throw new Exception("Pattern builder already set up.");
            WasSetup = true;
            ActionSet.ContinueSkip.Setup(ActionSet);

            Start = new SkipEndMarker();
            ActionSet.AddAction(Start);
        }

        public virtual void Finish()
        {
            if (!WasSetup) throw new Exception("Pattern builder not set up yet.");
            ActionSet.ContinueSkip.SetSkipCount(ActionSet, Start);
            
            if (Condition != null)
                ActionSet.AddAction(Element.Part<A_LoopIf>(Element.Part<V_Not>(Condition)));
            else
                ActionSet.AddAction(Element.Part<A_Loop>());

            ActionSet.ContinueSkip.ResetSkipCount(ActionSet);
        }
    }

    public class ForeachBuilder : WhileBuilder
    {
        private IndexReference IndexStore { get; }
        public IWorkshopTree Array { get; }
        public IWorkshopTree Index { get; }
        public IWorkshopTree IndexValue { get; }

        public ForeachBuilder(ActionSet actionSet, IWorkshopTree array) : base(actionSet)
        {
            IndexStore = actionSet.VarCollection.Assign("foreachIndex,", actionSet.IsGlobal, true);
            Array = array;
            Condition = new V_Compare(IndexStore.GetVariable(), Operators.LessThan, Element.Part<V_CountOf>(Array));
            Index = IndexStore.GetVariable();
            IndexValue = Element.Part<V_ValueInArray>(Array, IndexStore.GetVariable());
        }

        public override void Finish()
        {
            ActionSet.AddAction(IndexStore.ModifyVariable(Operation.Add, 1));
            base.Finish();
        }
    }
}