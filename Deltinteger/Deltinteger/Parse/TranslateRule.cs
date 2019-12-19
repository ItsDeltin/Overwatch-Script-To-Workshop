using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class TranslateRule
    {
        public readonly List<IActionList> Actions = new List<IActionList>();
        public ActionSet ActionSet { get; }
        public DeltinScript DeltinScript { get; }
        public bool IsGlobal { get; }
        public ContinueSkip ContinueSkip { get; }
        public List<MethodStack> MethodStack { get; } = new List<MethodStack>();

        private string Name { get; }
        private RuleEvent EventType { get; }
        private Team Team { get; }
        private PlayerSelector Player { get; }
        private bool Disabled { get; }

        public TranslateRule(DeltinScript deltinScript, RuleAction ruleAction)
        {
            DeltinScript = deltinScript;
            IsGlobal = ruleAction.EventType == RuleEvent.OngoingGlobal;
            Name = ruleAction.Name;
            EventType = ruleAction.EventType;
            Team = ruleAction.Team;
            Player = ruleAction.Player;
            Disabled = ruleAction.Disabled;
            ContinueSkip = new ContinueSkip(this);

            ActionSet = new ActionSet(this, null, Actions);
            ruleAction.Block.Translate(ActionSet);
        }

        public TranslateRule(DeltinScript deltinScript, string name, RuleEvent eventType, Team team, PlayerSelector player, bool disabled = false)
        {
            DeltinScript = deltinScript;
            IsGlobal = eventType == RuleEvent.OngoingGlobal;
            Name = name;
            EventType = eventType;
            Team = team;
            Player = player;
            Disabled = disabled;
            ContinueSkip = new ContinueSkip(this);
            ActionSet = new ActionSet(this, null, Actions);
        }

        public TranslateRule(DeltinScript deltinScript, string name, RuleEvent eventType) : this(deltinScript, name, eventType, Team.All, PlayerSelector.All)
        {
        }

        public Rule GetRule()
        {
            Rule rule = new Rule(Name, EventType, Team, Player);
            rule.Actions = GetActions();
            rule.Disabled = Disabled;
            return rule;
        }

        private Element[] GetActions()
        {
            List<Element> actions = new List<Element>();

            foreach (IActionList action in this.Actions)
                if (action.IsAction)
                    actions.Add(action.GetAction());

            return actions.ToArray();
        }
    }

    public class ActionSet
    {
        public TranslateRule Translate { get; private set; }
        public DocRange GenericErrorRange { get; private set; }
        public VarIndexAssigner IndexAssigner { get; private set; }
        public ReturnHandler ReturnHandler { get; private set; }
        public bool IsGlobal                => Translate.IsGlobal;
        public List<IActionList> ActionList => Translate.Actions;
        public VarCollection VarCollection  => Translate.DeltinScript.VarCollection;
        public ContinueSkip ContinueSkip    => Translate.ContinueSkip;

        public ActionSet(TranslateRule translate, DocRange genericErrorRange, List<IActionList> actions)
        {
            Translate = translate;
            GenericErrorRange = genericErrorRange;
            IndexAssigner = translate.DeltinScript.DefaultIndexAssigner;
        }
        private ActionSet(ActionSet other)
        {
            Translate = other.Translate;
            GenericErrorRange = other.GenericErrorRange;
            IndexAssigner = other.IndexAssigner;
            ReturnHandler = other.ReturnHandler;
        }
        private ActionSet Clone()
        {
            return new ActionSet(this);
        }

        public ActionSet New(DocRange range)
        {
            var newActionSet = Clone();
            newActionSet.GenericErrorRange = range ?? throw new ArgumentNullException(nameof(range));
            return newActionSet;
        }
        public ActionSet New(VarIndexAssigner indexAssigner)
        {
            var newActionSet = Clone();
            newActionSet.IndexAssigner = indexAssigner ?? throw new ArgumentNullException(nameof(indexAssigner));
            return newActionSet;
        }
        public ActionSet New(ReturnHandler returnHandler)
        {
            var newActionSet = Clone();
            newActionSet.ReturnHandler = returnHandler ?? throw new ArgumentNullException(nameof(returnHandler));
            return newActionSet;
        }

        public void AddAction(IWorkshopTree action)
        {
            ActionList.Add(new ALAction(action));
        }
        public void AddAction(IWorkshopTree[] actions)
        {
            foreach (var action in actions)
                ActionList.Add(new ALAction(action));
        }
        public void AddAction(IActionList action)
        {
            ActionList.Add(action);
        }
        public void AddAction(IActionList[] actions)
        {
            ActionList.AddRange(actions);
        }
    }

    public interface IActionList
    {
        bool IsAction { get; }
        Element GetAction();
    }

    public class ALAction : IActionList
    {
        public IWorkshopTree Calling { get; }
        public bool IsAction { get; } = true;

        public ALAction(IWorkshopTree calling)
        {
            Calling = calling;
        }

        public Element GetAction()
        {
            return (Element)Calling;
        }
    }

    public class SkipStartMarker : IActionList
    {
        public IWorkshopTree Condition { get; }
        private ActionSet ActionSet { get; }
        public IWorkshopTree SkipCount { get; set; }
        public bool IsAction { get; } = true;

        public SkipStartMarker(ActionSet actionSet, IWorkshopTree condition)
        {
            ActionSet = actionSet;
            Condition = condition;
        }
        public SkipStartMarker(ActionSet actionSet)
        {
            ActionSet = actionSet;
        }

        public V_Number GetSkipCount(SkipEndMarker marker)
        {
            int count = 0;
            bool foundStart = false;
            for (int i = 0; i < ActionSet.ActionList.Count; i++)
            {
                if (object.ReferenceEquals(ActionSet.ActionList[i], this))
                {
                    if (foundStart) throw new Exception("Skip start marker is on the action list multiple times.");
                    foundStart = true;
                }

                if (object.ReferenceEquals(ActionSet.ActionList[i], marker))
                {
                    if (!foundStart) throw new Exception("Skip start marker not found.");
                    break;
                }

                if (foundStart && ActionSet.ActionList[i].IsAction)
                    count++;
            }

            return new V_Number(count - 1);
        }

        public Element GetAction()
        {
            if (Condition == null)
                return Element.Part<A_Skip>(SkipCount);
            else
                return Element.Part<A_SkipIf>(Element.Part<V_Not>(Condition), SkipCount);
        }
    }

    public class SkipEndMarker : IActionList
    {
        public bool IsAction { get; } = false;

        public Element GetAction() => throw new NotImplementedException();
    }
}