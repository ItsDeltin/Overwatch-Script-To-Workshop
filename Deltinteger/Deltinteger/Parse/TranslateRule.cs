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
        public List<MethodStack> MethodStack { get; } = new List<MethodStack>();
        
        public List<Condition> Conditions { get; } = new List<Condition>();

        private string Name { get; }
        private RuleEvent EventType { get; }
        private Team Team { get; }
        private PlayerSelector Player { get; }
        private bool Disabled { get; }
        private double Priority { get; }
        private Subroutine Subroutine { get; }

        public TranslateRule(DeltinScript deltinScript, RuleAction ruleAction)
        {
            DeltinScript = deltinScript;
            IsGlobal = ruleAction.EventType == RuleEvent.OngoingGlobal;
            Name = ruleAction.Name;
            EventType = ruleAction.EventType;
            Team = ruleAction.Team;
            Player = ruleAction.Player;
            Disabled = ruleAction.Disabled;
            Priority = ruleAction.Priority;

            ActionSet = new ActionSet(this, null, Actions);

            GetConditions(ruleAction);

            ReturnHandler returnHandler = new ReturnHandler(ActionSet, Name, false);
            ruleAction.Block.Translate(ActionSet.New(returnHandler));
            returnHandler.ApplyReturnSkips();
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
            ActionSet = new ActionSet(this, null, Actions);
        }
        public TranslateRule(DeltinScript deltinScript, string name, RuleEvent eventType) : this(deltinScript, name, eventType, Team.All, PlayerSelector.All) {}
        public TranslateRule(DeltinScript deltinScript, string name) : this(deltinScript, name, RuleEvent.OngoingGlobal, Team.All, PlayerSelector.All) {}
        public TranslateRule(DeltinScript deltinScript, string name, Subroutine subroutine) : this(deltinScript, name, RuleEvent.Subroutine)
        {
            Subroutine = subroutine;
        }

        private void GetConditions(RuleAction ruleAction)
        {
            foreach (var condition in ruleAction.Conditions)
            {
                var conditionParse = condition.Expression.Parse(ActionSet);

                Element value1;
                EnumMember compareOperator;
                Element value2;

                if (conditionParse is V_Compare)
                {
                    value1 = (Element)((Element)conditionParse).ParameterValues[0];
                    compareOperator = (EnumMember)((Element)conditionParse).ParameterValues[1];
                    value2 = (Element)((Element)conditionParse).ParameterValues[2];
                }
                else
                {
                    value1 = (Element)conditionParse;
                    compareOperator = EnumData.GetEnumValue(Operators.Equal);
                    value2 = new V_True();
                }

                Conditions.Add(new Condition(value1, compareOperator, value2));
            }
        }

        public Rule GetRule()
        {
            Rule rule;
            if (Subroutine == null)
                rule = new Rule(Name, EventType, Team, Player);
            else
                rule = new Rule(Name, Subroutine);
            rule.Actions = GetActions();
            rule.Conditions = Conditions.ToArray();
            rule.Disabled = Disabled;
            rule.Priority = Priority;
            return rule;
        }

        private Element[] GetActions()
        {
            List<Element> actions = new List<Element>();

            bool doLoop = false;
            do
            {
                bool anyRemoved = false;

                for (int i = 0; i < Actions.Count && !anyRemoved; i++)
                    if (Actions[i].ShouldRemove())
                    {
                        Actions.RemoveAt(i);
                        anyRemoved = true;
                    }
                
                doLoop = anyRemoved;
            }
            while (doLoop);

            foreach (IActionList action in this.Actions)
                if (action.IsAction)
                    actions.Add(action.GetAction());

            return actions.ToArray();
        }
    
        public bool WasCalled(IApplyBlock callable) => MethodStack.Any(ms => ms.Function == this);
    }

    public class ActionSet
    {
        public TranslateRule Translate { get; private set; }
        public DocRange GenericErrorRange { get; private set; }
        public VarIndexAssigner IndexAssigner { get; private set; }
        public ReturnHandler ReturnHandler { get; private set; }
        public Element CurrentObject { get; private set; }
        public int IndentCount { get; private set; }
        public bool IsGlobal { get; }
        public List<IActionList> ActionList { get; }
        public VarCollection VarCollection { get; }

        public int ActionCount {
            get {
                return ActionList.Count;
            }
        }

        public ActionSet(bool isGlobal, VarCollection varCollection)
        {
            IsGlobal = isGlobal;
            VarCollection = varCollection;
            ActionList = new List<IActionList>();
        }
        public ActionSet(TranslateRule translate, DocRange genericErrorRange, List<IActionList> actions)
        {
            Translate = translate;
            IsGlobal = translate.IsGlobal;
            ActionList = translate.Actions;
            VarCollection = translate.DeltinScript.VarCollection;

            GenericErrorRange = genericErrorRange;
            IndexAssigner = translate.DeltinScript.DefaultIndexAssigner;
        }
        private ActionSet(ActionSet other)
        {
            Translate = other.Translate;
            IsGlobal = other.IsGlobal;
            ActionList = other.ActionList;
            VarCollection = other.VarCollection;

            GenericErrorRange = other.GenericErrorRange;
            IndexAssigner = other.IndexAssigner;
            ReturnHandler = other.ReturnHandler;
            CurrentObject = other.CurrentObject;
            IndentCount = other.IndentCount;
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
        public ActionSet New(Element currentObject)
        {
            var newActionSet = Clone();
            newActionSet.CurrentObject = currentObject;
            return newActionSet;
        }
        public ActionSet Indent()
        {
            var newActionSet = Clone();
            newActionSet.IndentCount++;
            return newActionSet;
        }

        public void AddAction(IWorkshopTree action)
        {
            if (action is Element element) element.Indent = IndentCount;

            ActionList.Add(new ALAction(action));
        }
        public void AddAction(IWorkshopTree[] actions)
        {
            foreach (var action in actions)
            {
                if (action is Element element) element.Indent = IndentCount;

                ActionList.Add(new ALAction(action));
            }
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
        bool ShouldRemove();
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

        public bool ShouldRemove() => false;
    }

    public class SkipStartMarker : IActionList
    {
        public IWorkshopTree Condition { get; }
        private ActionSet ActionSet { get; }
        public Element SkipCount { get; private set; }
        public SkipEndMarker EndMarker { get; private set; }
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

        public void SetEndMarker(SkipEndMarker endMarker)
        {
            if (SkipCount != null) throw new Exception("SkipCount not null.");
            EndMarker = endMarker;
        }

        public void SetSkipCount(Element count)
        {
            if (EndMarker != null) throw new Exception("EndMarker not null.");
            SkipCount = count;
        }

        public Element GetAction()
        {
            Element skipCount;
            if (SkipCount != null) skipCount = SkipCount;
            else skipCount = GetSkipCount(EndMarker);

            if (Condition == null)
                return Element.Part<A_Skip>(skipCount);
            else
                return Element.Part<A_SkipIf>(Element.Part<V_Not>(Condition), skipCount);
        }

        public bool ShouldRemove() => GetSkipCount(EndMarker).Value == 0;
    }

    public class SkipEndMarker : IActionList
    {
        public bool IsAction { get; } = false;
        public Element GetAction() => throw new NotImplementedException();
        public bool ShouldRemove() => false;
    }
}