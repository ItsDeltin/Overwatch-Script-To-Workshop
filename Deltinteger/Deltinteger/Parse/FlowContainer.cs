using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public interface IContinueContainer
    {
        void AddContinue(string comment);
    }

    public interface IBreakContainer
    {
        void AddBreak(string comment);
    }

    class LoopFlowHelper : IContinueContainer, IBreakContainer
    {
        readonly List<SkipStartMarker> continues = new List<SkipStartMarker>();
        readonly List<SkipStartMarker> breaks = new List<SkipStartMarker>();
        readonly bool useWorkshopContinue;
        readonly bool useWorkshopBreak;
        readonly ActionSet actionSet;

        // If statements nested in while loops will cause the workshop's Continue action
        // to restart right before the if statement instead of at the end of the loop.
        const bool ContinueWorkaround = true;

        public LoopFlowHelper(ActionSet actionSet, bool useWorkshopContinue = true, bool useWorkshopBreak = true)
        {
            this.useWorkshopContinue = useWorkshopContinue;
            this.useWorkshopBreak = useWorkshopBreak;
            this.actionSet = actionSet;
        }

        public void AddContinue(string comment)
        {
            if (useWorkshopContinue && !ContinueWorkaround)
            {
                Element con = Element.Part("Continue");
                con.Comment = comment;
                actionSet.AddAction(con);
            }
            else
            {
                SkipStartMarker continuer = new SkipStartMarker(actionSet, comment);
                actionSet.AddAction(continuer);
                continues.Add(continuer);
            }
        }

        public void AddBreak(string comment)
        {
            if (useWorkshopBreak)
            {
                Element brk = Element.Part("Break");
                brk.Comment = comment;
                actionSet.AddAction(brk);
            }
            else
            {
                SkipStartMarker breaker = new SkipStartMarker(actionSet, comment);
                actionSet.AddAction(breaker);
                breaks.Add(breaker);
            }
        }

        public void ContinueToHere() => SetSkips(continues);

        public void BreakToHere() => SetSkips(breaks);

        public void SetSkips(List<SkipStartMarker> skips)
        {
            // Create the end marker that marks the spot right before the End action (if continuing) or right after the End action (if breaking).
            SkipEndMarker endMarker = new SkipEndMarker();

            // Add the end marker to the action set.
            actionSet.AddAction(endMarker);

            // Assign the end marker to the continue/break skips.
            foreach (SkipStartMarker startMarker in skips)
                startMarker.SetEndMarker(endMarker);
        }
    }
}