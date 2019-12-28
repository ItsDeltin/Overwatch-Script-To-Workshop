using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.CustomMethods
{
    abstract class TestCondition : CustomMethodBase
    {
        protected bool TestingIfTrue;

        protected TestCondition(bool testingIfTrue)
        {
            TestingIfTrue = testingIfTrue;
        }

        public override CodeParameter[] Parameters() => null;

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            // Setup the continue skip.
            ContinueSkip continueSkip = actionSet.ContinueSkip;
            continueSkip.Setup(actionSet);

            IndexReference result = actionSet.VarCollection.Assign($"_conditionTestResult", actionSet.IsGlobal, true);

            continueSkip.SetSkipCount(actionSet, continueSkip.GetSkipCount(actionSet) + 3);
            actionSet.AddAction(ArrayBuilder<Element>.Build(
                // This will continue at (0) if the rule loops.
                new A_LoopIfConditionIsFalse(),
                // Set the result to true.
                result.SetVariable(new V_True()),
                Element.Part<A_Skip>(new V_Number(1)),

                // The rule will loop back here (0) if false.
                result.SetVariable(new V_False())
            ));
            continueSkip.ResetSkipCount(actionSet);

            if (TestingIfTrue)
                return result.GetVariable();
            else
                return Element.Part<V_Not>(result.GetVariable());
        }
    }

    [CustomMethod("IsConditionTrue", "Determines if the conditions is true. Has a 0.016 second delay.", CustomMethodType.MultiAction_Value)]
    class IsConditionTrue : TestCondition
    {
        public IsConditionTrue() : base(true) {}
    }

    [CustomMethod("IsConditionFalse", "Determines if the conditions is false. Has a 0.016 second delay.", CustomMethodType.MultiAction_Value)]
    class IsConditionFalse : TestCondition
    {
        public IsConditionFalse() : base(false) {}
    }
}