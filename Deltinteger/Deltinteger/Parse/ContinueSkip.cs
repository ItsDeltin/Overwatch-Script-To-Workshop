using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class ContinueSkip
    {
        private const int ExpectedActionCount = 5;

        private readonly bool IsGlobal;
        private readonly List<Element> Actions;
        private readonly VarCollection VarCollection;

        private IndexedVar SkipCount;
        private IndexedVar TempHolder;

        private bool IsSetup = false;

        public ContinueSkip(bool isGlobal, List<Element> actions, VarCollection varCollection)
        {
            IsGlobal = isGlobal;
            Actions = actions;
            VarCollection = varCollection;
        }

        public void Setup()
        {
            if (IsSetup)
                return;
            IsSetup = true;

            SkipCount = VarCollection.AssignVar(null, "ContinueSkip", IsGlobal, null);
            TempHolder = VarCollection.AssignVar(null, "ContinueSkip temp holder", IsGlobal, null);
            
            A_Wait waitAction = A_Wait.MinimumWait;
            waitAction.Comment = "ContinueSkip Wait";

            A_SkipIf skipAction = Element.Part<A_SkipIf>
            (
                // Condition
                Element.Part<V_Compare>(SkipCount.GetVariable(), EnumData.GetEnumValue(Operators.Equal), new V_Number(0)),
                // Number of actions
                new V_Number(3)
            );
            skipAction.Comment = "ContinueSkip Skipper";

            Element[] actions = ArrayBuilder<Element>.Build(
                waitAction,
                skipAction,
                TempHolder.SetVariable(SkipCount.GetVariable()),
                SkipCount.SetVariable(0),
                Element.Part<A_Skip>(TempHolder.GetVariable())
            );

            if (actions.Length != ExpectedActionCount)
                throw new Exception($"Expected {ExpectedActionCount} actions for the Continue Skip, got {actions.Length} instead.");

            Actions.InsertRange(0, actions);
        }
        

        public void SetSkipCount(int number)
        {
            CheckSetup();
            Actions.AddRange(SetSkipCountActions(number));
        }

        public Element[] SetSkipCountActions(int number)
        {
            return SkipCount.SetVariable(number);
        }

        public void SetSkipCount(Element element)
        {
            CheckSetup();
            Actions.AddRange(SkipCount.SetVariable(element));
        }

        public void ResetSkip()
        {
            CheckSetup();
            SetSkipCount(0);
        }

        public Element[] ResetSkipActions()
        {
            CheckSetup();
            return SetSkipCountActions(0);
        }

        public int GetSkipCount()
        {
            // Gets the skip count based on the number of actions and the position of the coninue skip's skip-if.
            // This will need to be changed if any other components are added that insert actions into the ruleset.
            return Actions.Count - (IsSetup ? ExpectedActionCount : 0);
        }

        private void CheckSetup()
        {
            if (!IsSetup)
                throw new Exception("ContinueSkip not set up.");
        }
    }
}