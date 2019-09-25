using System;
using Deltin.Deltinteger.WorkshopWiki;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Elements
{
    abstract class TestCondition : CustomMethodBase
    {
        protected bool TestingIfTrue;
        private string Name { get; }

        protected TestCondition(bool testingIfTrue)
        {
            TestingIfTrue = testingIfTrue;
            Name = $"IsCondition{TestingIfTrue.ToString()}";
        }

        protected override MethodResult Get()
        {
            // Setup the continue skip.
            ContinueSkip continueSkip = TranslateContext.ContinueSkip;
            continueSkip.Setup();

            IndexedVar result = TranslateContext.VarCollection.AssignVar(Scope, $"{Name} result", TranslateContext.IsGlobal, null);

            Element[] actions = ArrayBuilder<Element>.Build(

                // Set the continue skip.
                continueSkip.SetSkipCountActions(continueSkip.GetSkipCount() + 4),

                // This will continue at (0) if the rule loops.
                new A_LoopIfConditionIsFalse(),
                // Set the result to true.
                result.SetVariable(new V_True()),
                Element.Part<A_Skip>(new V_Number(1)),

                // The rule will loop back here (0) if false.
                result.SetVariable(new V_False()),

                // Reset the continueskip
                continueSkip.ResetSkipActions()
            );

            if (TestingIfTrue)
                return new MethodResult(actions, result.GetVariable());
            else
                return new MethodResult(actions, !(result.GetVariable()));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki($"Determines if the condition is {TestingIfTrue.ToString().ToLower()}. Has a {Constants.MINIMUM_WAIT} second delay.");
        }
    }

    [CustomMethod("IsConditionTrue", CustomMethodType.MultiAction_Value)]
    class IsConditionTrue : TestCondition
    {
        public IsConditionTrue() : base(true) {}
    }

    [CustomMethod("IsConditionFalse", CustomMethodType.MultiAction_Value)]
    class IsConditionFalse : TestCondition
    {
        public IsConditionFalse() : base(false) {}
    }
}