using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class ForeachBuilder
    {
        private ActionSet ActionSet { get; }
        public IndexReference IndexStore { get; }
        private Element Condition { get; }
        public IWorkshopTree Array { get; }
        public Element Index { get; }
        public Element IndexValue { get; }
        private bool Recursive { get; }

        public ForeachBuilder(ActionSet actionSet, IWorkshopTree array, bool recursive = false)
        {
            ActionSet = actionSet;
            IndexStore = actionSet.VarCollection.Assign("foreachIndex,", actionSet.IsGlobal, !recursive);
            Recursive = recursive;

            if (recursive)
            {
                RecursiveIndexReference recursiveStore = new RecursiveIndexReference(IndexStore);
                IndexStore = recursiveStore;

                actionSet.InitialSet().AddAction(recursiveStore.Reset());
                actionSet.AddAction(recursiveStore.Push(0));
                actionSet.ReturnHandler.AdditionalPopOnReturn.Add(recursiveStore);
            }
            else
                actionSet.AddAction(IndexStore.SetVariable(0));

            Array = array;
            Condition = new V_Compare(IndexStore.GetVariable(), Operators.LessThan, Element.Part<V_CountOf>(Array));
            Index = (Element)IndexStore.GetVariable();
            IndexValue = Element.Part<V_ValueInArray>(Array, IndexStore.GetVariable());
            
            actionSet.AddAction(Element.Part<A_While>(Condition));
        }

        public void Finish()
        {
            ActionSet.AddAction(IndexStore.ModifyVariable(Operation.Add, 1));
            ActionSet.AddAction(new A_End());

            if (Recursive)
            {
                ActionSet.AddAction(((RecursiveIndexReference)IndexStore).Pop());
                ActionSet.ReturnHandler.AdditionalPopOnReturn.Remove((RecursiveIndexReference)IndexStore);
            }
        }
    }

    public class SwitchBuilder
    {
        public bool AutoBreak = true;

        readonly SkipStartMarker Skipper;
        readonly List<IWorkshopTree> skipCounts = new List<IWorkshopTree>();
        readonly List<IWorkshopTree> skipValues = new List<IWorkshopTree>();
        public readonly List<SkipStartMarker> SkipToEnd = new List<SkipStartMarker>();
        readonly ActionSet actionSet;
        int LastCaseStart = -1;
        SwitchBuilderState State = SwitchBuilderState.Start;
        bool defaultAdded;

        public SwitchBuilder(ActionSet actionSet)
        {
            this.actionSet = actionSet;

            // Create the switch skipper.
            Skipper = new SkipStartMarker(actionSet);
            actionSet.AddAction(Skipper);
        }

        public void NextCase(IWorkshopTree value)
        {
            // If the state is on a case and the action count was changed, create new skip that will skip to the end of the switch.
            if (AutoBreak && State == SwitchBuilderState.OnCase && actionSet.ActionCount != LastCaseStart)
            {
                // Create the skip and add it to the actionset.
                SkipStartMarker skipToEnd = new SkipStartMarker(actionSet);
                actionSet.AddAction(skipToEnd);

                // Add it to the list of skips that need to skip to the end.
                SkipToEnd.Add(skipToEnd);
            }

            // Update the state.
            State = SwitchBuilderState.OnCase;

            // Mark the start of the case.
            SkipEndMarker startCase = new SkipEndMarker();
            actionSet.AddAction(startCase);

            // Add the skip length to the start of the case to the skipCounts.
            skipCounts.Add(Skipper.GetSkipCount(startCase));

            // Add the skip value.
            skipValues.Add(value);

            // Update the number of actions.
            LastCaseStart = actionSet.ActionCount;
        }

        public void AddDefault()
        {
            if (defaultAdded) throw new Exception("Default already added.");
            defaultAdded = true;

            // Mark the start of the case.
            SkipEndMarker startCase = new SkipEndMarker();
            actionSet.AddAction(startCase);
            skipCounts.Insert(0, Skipper.GetSkipCount(startCase));
            State = SwitchBuilderState.OnCase;
        }

        public void Finish(Element switchValue)
        {
            // Set state to finished.
            State = SwitchBuilderState.Finished;

            // Mark the end of the switch.
            SkipEndMarker switchEnd = new SkipEndMarker();
            actionSet.AddAction(switchEnd);

            // Update switch skips to skip to the end of the switch.
            foreach (SkipStartMarker skipToEnd in SkipToEnd)
                skipToEnd.SetEndMarker(switchEnd);
            
            // Default insert.
            // TODO: Default case
            if (!defaultAdded)
                skipCounts.Insert(0, Skipper.GetSkipCount(switchEnd));
            
            // Skip to the case.
            Skipper.SetSkipCount(
                // Create an array of all skip counts.
                Element.CreateArray(skipCounts.ToArray())[
                    // Get the case with the value that matches.
                    // IndexOfArrayValue will return -1 if the case is not found,
                    // Add 1 and skip to the default case.
                    Element.Part<V_IndexOfArrayValue>(
                        Element.CreateArray(skipValues.ToArray()),
                        switchValue
                    ) + 1
                ]
            );
        }
    }

    enum SwitchBuilderState
    {
        Start,
        OnCase,
        Finished
    }

    public class ForRangeBuilder
    {
        public ActionSet ActionSet { get; }
        public string ControlVariableName { get; }
        public Element Start { get; }
        public Element End { get; }
        public Element Step { get; }
        public bool Extended { get; }
        public IndexReference Iterator { get; private set; }

        public ForRangeBuilder(ActionSet actionSet, string controlVariable, Element start, Element end, Element step, bool extended = false)
        {
            ActionSet = actionSet;
            ControlVariableName = controlVariable;
            Start = start;
            End = end;
            Step = step;
            Extended = extended;
        }

        public ForRangeBuilder(ActionSet actionSet, string controlVariable, Element end, bool extended = false)
        {
            ActionSet = actionSet;
            ControlVariableName = controlVariable;
            End = end;
            Start = 0;
            Step = 1;
            Extended = extended;
        }

        public void Init()
        {
            Iterator = ActionSet.VarCollection.Assign(ControlVariableName, ActionSet.IsGlobal, Extended);
            
            // Extended variable uses a while loop.
            if (Extended)
            {
                ActionSet.AddAction(Iterator.SetVariable(Start));
                ActionSet.AddAction(Element.Part<A_While>(new V_Compare(
                    Iterator.Get(),
                    Operators.LessThan,
                    End
                )));
            }
            else
            {
                // Global
                if (ActionSet.IsGlobal)
                    ActionSet.AddAction(Element.Part<A_ForGlobalVariable>(Iterator.WorkshopVariable, Start, End, Step));
                // Player
                else
                    ActionSet.AddAction(Element.Part<A_ForPlayerVariable>(new V_EventPlayer(), Iterator.WorkshopVariable, Start, End, Step));
            }
        }

        public void Finish()
        {
            if (Extended)
            {
                ActionSet.AddAction(Iterator.ModifyVariable(Operation.Add, Step));
                ActionSet.AddAction(new A_End());
            }
            else
            {
                ActionSet.AddAction(new A_End());
            }
        }

        public Element Value => Iterator.Get();
        public static implicit operator Element(ForRangeBuilder range) => range.Value;
    }
}