using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class ForeachBuilder
    {
        public IWorkshopTree IndexValue { get; }

        readonly IndexReference forIndex;
        readonly ActionSet actionSet;
        readonly bool recursive;
        readonly bool extended;

        public ForeachBuilder(ActionSet actionSet, IWorkshopTree array, bool recursive = false, bool extended = true)
        {
            this.actionSet = actionSet;
            this.recursive = recursive;
            this.extended = extended;
            forIndex = actionSet.VarCollection.Assign("foreachIndex", actionSet.IsGlobal, extended);

            // Initialize recursive variable.
            if (recursive)
            {
                RecursiveIndexReference recursiveFor = new RecursiveIndexReference(forIndex);
                forIndex = recursiveFor;

                actionSet.InitialSet().AddAction(recursiveFor.Reset());
                actionSet.AddAction(recursiveFor.Push(0));
                actionSet.ReturnHandler.AdditionalPopOnReturn.Add(recursiveFor);
            }
            // Initialize extended variable.
            else if (extended)
                actionSet.AddAction(forIndex.SetVariable(0));

            var arrayLength = Element.CountOf(StructHelper.ExtractArbritraryValue(array));

            if (recursive || extended)
            {
                var condition = Element.Compare(forIndex.GetVariable(), Operator.LessThan, arrayLength);
                actionSet.AddAction(Element.While(condition));
            }
            else
            {
                if (actionSet.IsGlobal)
                    actionSet.AddAction(Element.ForGlobalVariable(forIndex.WorkshopVariable, 0, arrayLength, 1));
                else
                    actionSet.AddAction(Element.ForPlayerVariable(Element.EventPlayer(), forIndex.WorkshopVariable, 0, arrayLength, 1));
            }

            IndexValue = StructHelper.ValueInArray(array, forIndex.GetVariable());
        }

        public void Finish()
        {
            // Not using auto-for, manually increment variable.
            if (recursive || extended)
                actionSet.AddAction(forIndex.ModifyVariable(Operation.Add, 1));
            // End block.
            actionSet.AddAction(Element.End());

            if (recursive)
            {
                actionSet.AddAction(((RecursiveIndexReference)forIndex).Pop());
                actionSet.ReturnHandler.AdditionalPopOnReturn.Remove((RecursiveIndexReference)forIndex);
            }
        }
    }

    public class ForBuilder
    {
        public IndexReference Variable { get; }
        public Element Value => Variable.Get();
        private readonly ActionSet _actionSet;
        private readonly Element _end;

        public ForBuilder(ActionSet actionSet, string variableName, Element end)
        {
            Variable = actionSet.VarCollection.Assign(variableName, actionSet.IsGlobal, false);
            _actionSet = actionSet;
            _end = end;
        }

        public void Init()
        {
            var var = Variable.WorkshopVariable;

            if (var.IsGlobal)
                _actionSet.AddAction(Element.ForGlobalVariable(var, (Element)0, _end, (Element)1));
            else
                _actionSet.AddAction(Element.ForPlayerVariable(Element.EventPlayer(), var, (Element)0, _end, (Element)1));
        }

        public void End() => _actionSet.AddAction(Element.End());
    }

    public class SwitchBuilder : IBreakContainer
    {
        /// <summary>Determines if fall-through is enabled.</summary>
        readonly bool autoBreak;
        /// <summary>The initial skip action.</summary>
        readonly SkipStartMarker skipper;
        /// <summary>Contains the number of actions until each case.</summary>
        readonly List<IWorkshopTree> skipCounts = new List<IWorkshopTree>();
        /// <summary>The value required to select a case.</summary>
        readonly List<IWorkshopTree> skipValues = new List<IWorkshopTree>();
        /// <summary>Breaks and end-of-cases used to jump to the end of the switch.</summary>
        readonly List<SkipStartMarker> skipToEnd = new List<SkipStartMarker>();
        /// <summary>The current rule's action set.</summary>
        readonly ActionSet actionSet;
        /// <summary>Used to track action counts for auto-break.</summary>
        int lastCaseStart = -1;
        /// <summary>The switch builder's current state.</summary>
        SwitchBuilderState state = SwitchBuilderState.Start;
        /// <summary>Determines if the default case was added yet.</summary>
        bool defaultAdded;

        public SwitchBuilder(ActionSet actionSet, bool autoBreak = true)
        {
            this.actionSet = actionSet;
            this.autoBreak = autoBreak;

            // Create the switch skipper.
            skipper = new SkipStartMarker(actionSet);
            actionSet.AddAction(skipper);
        }

        public void NextCase(IWorkshopTree value)
        {
            // If the state is on a case and the action count was changed, create new skip that will skip to the end of the switch.
            if (autoBreak && state == SwitchBuilderState.OnCase && actionSet.ActionCount != lastCaseStart)
            {
                // Create the skip and add it to the actionset.
                SkipStartMarker skipToEnd = new SkipStartMarker(actionSet);
                actionSet.AddAction(skipToEnd);

                // Add it to the list of skips that need to skip to the end.
                this.skipToEnd.Add(skipToEnd);
            }

            // Update the state.
            state = SwitchBuilderState.OnCase;

            // Mark the start of the case.
            SkipEndMarker startCase = new SkipEndMarker();
            actionSet.AddAction(startCase);

            // Add the skip length to the start of the case to the skipCounts.
            skipCounts.Add(skipper.GetSkipCount(startCase));

            // Add the skip value.
            skipValues.Add(value);

            // Update the number of actions.
            lastCaseStart = actionSet.ActionCount;
        }

        public void AddDefault()
        {
            if (defaultAdded) throw new Exception("Default already added.");
            defaultAdded = true;

            // Mark the start of the case.
            SkipEndMarker startCase = new SkipEndMarker();
            actionSet.AddAction(startCase);
            skipCounts.Insert(0, skipper.GetSkipCount(startCase));
            state = SwitchBuilderState.OnCase;
        }

        public void Finish(Element switchValue)
        {
            // Set state to finished.
            state = SwitchBuilderState.Finished;

            // Mark the end of the switch.
            SkipEndMarker switchEnd = new SkipEndMarker();
            actionSet.AddAction(switchEnd);

            // Update switch skips to skip to the end of the switch.
            foreach (SkipStartMarker skipToEnd in skipToEnd)
                skipToEnd.SetEndMarker(switchEnd);

            // Default insert.
            // TODO: Default case
            if (!defaultAdded)
                skipCounts.Insert(0, skipper.GetSkipCount(switchEnd));

            // Skip to the case.
            skipper.SetSkipCount(
                // Create an array of all skip counts.
                Element.CreateArray(skipCounts.ToArray())[
                    // Get the case with the value that matches.
                    // IndexOfArrayValue will return -1 if the case is not found,
                    // Add 1 and skip to the default case.
                    Element.IndexOfArrayValue(
                        Element.CreateArray(skipValues.ToArray()),
                        switchValue
                    ) + 1
                ]
            );
        }

        public void AddBreak(string comment)
        {
            SkipStartMarker breaker = new SkipStartMarker(actionSet, comment);
            actionSet.AddAction(breaker);
            skipToEnd.Add(breaker);
        }
    }

    enum SwitchBuilderState
    {
        Start,
        OnCase,
        Finished
    }

    public struct IfBuilder
    {
        readonly ActionSet actionSet;

        private IfBuilder(ActionSet actionSet) => this.actionSet = actionSet;

        public static IfBuilder If(ActionSet actionSet, Element condition, Action addBlock)
        {
            actionSet.AddAction(Element.If(condition));
            addBlock();
            return new IfBuilder(actionSet);
        }

        public IfBuilder Else(Action addBlock)
        {
            actionSet.AddAction(Element.Else());
            addBlock();
            return this;
        }

        public void Ok()
        {
            actionSet.AddAction(Element.End());
        }
    }
}