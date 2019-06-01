using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    class ContinueSkip
    {
        private readonly bool IsGlobal;
        private readonly List<Element> Actions;

        private Var SkipCount;

        private bool IsSetup = false;

        public ContinueSkip(bool isGlobal, List<Element> actions)
        {
            IsGlobal = isGlobal;
            Actions = actions;
        }

        public void Setup()
        {
            if (IsSetup)
                return;
            IsSetup = true;

            SkipCount = Var.AssignVar(IsGlobal);
            
            // Add the required wait
            Actions.Insert(0, Element.Part<A_Wait>(new V_Number(Constants.MINIMUM_WAIT)));

            // Add the skip-if
            Actions.Insert(1, 
                Element.Part<A_SkipIf>
                (
                    Element.Part<V_Compare>(SkipCount.GetVariable(), Operators.NotEqual, new V_Number(0)),
                    SkipCount.GetVariable()
                )
            );
        }

        public Element SetSkipCount(int number)
        {
            CheckSetup();
            return SkipCount.SetVariable(new V_Number(number));
        }

        public Element ResetSkip()
        {
            CheckSetup();
            return SetSkipCount(0);
        }

        private void CheckSetup()
        {
            if (!IsSetup)
                throw new Exception("ContinueSkip not set up.");
        }
    }
}