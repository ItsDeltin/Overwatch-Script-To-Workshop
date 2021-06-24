using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Parse.Functions.Builder;
using Deltin.Deltinteger.Parse.Workshop;

namespace Deltin.Deltinteger.Parse
{
    public class TranslateRule
    {
        public readonly List<IActionList> Actions = new List<IActionList>();
        public ActionSet ActionSet { get; }
        public DeltinScript DeltinScript { get; }
        public bool IsGlobal { get; }
        public List<RecursiveStack> MethodStack { get; } = new List<RecursiveStack>();

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

            RuleReturnHandler returnHandler = new RuleReturnHandler(ActionSet);
            ruleAction.Block.Translate(ActionSet.New(returnHandler).ContainVariableAssigner());
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
        public TranslateRule(DeltinScript deltinScript, Subroutine subroutine, string name, bool defaultGlobal)
        {
            DeltinScript = deltinScript;
            IsGlobal = defaultGlobal;
            Name = name;
            EventType = RuleEvent.Subroutine;
            Subroutine = subroutine;
            ActionSet = new ActionSet(this, null, Actions);
        }
        public TranslateRule(DeltinScript deltinScript, string name, RuleEvent eventType) : this(deltinScript, name, eventType, Team.All, PlayerSelector.All) { }

        private void GetConditions(RuleAction ruleAction)
        {
            foreach (var condition in ruleAction.Conditions)
            {
                var conditionParse = condition.Expression.Parse(ActionSet);

                Element value1;
                Operator compareOperator;
                Element value2;

                if (conditionParse is Element asElement && asElement.Function.Name == "Compare")
                {
                    value1 = (Element)asElement.ParameterValues[0];
                    compareOperator = ((OperatorElement)asElement.ParameterValues[1]).Operator;
                    value2 = (Element)asElement.ParameterValues[2];
                }
                else
                {
                    value1 = (Element)conditionParse;
                    compareOperator = Operator.Equal;
                    value2 = Element.True();
                }

                Conditions.Add(new Condition(value1, compareOperator, value2) { Comment = condition.Comment?.GetContents() });
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
    }

    public class ActionSet
    {
        public TranslateRule Translate { get; private set; }
        public DeltinScript DeltinScript => Translate.DeltinScript;
        public VarIndexAssigner IndexAssigner { get; private set; }
        public ReturnHandler ReturnHandler { get; private set; }
        public IWorkshopTree CurrentObject { get; private set; }
        public SourceIndexReference CurrentObjectRelatedIndex { get; private set; }
        public IWorkshopTree This { get; private set; }
        public bool IsRecursive { get; private set; }
        public ActionComment CommentNext { get; private set; }
        public InstanceAnonymousTypeLinker ThisTypeLinker { get; private set; }
        public bool IsGlobal { get; }
        public List<IActionList> ActionList { get; }
        public VarCollection VarCollection { get; }
        public ToWorkshop ToWorkshop { get => Translate.DeltinScript.WorkshopConverter; }

        public int ActionCount => ActionList.Count;

        public ActionSet(TranslateRule translate, DocRange genericErrorRange, List<IActionList> actions)
        {
            Translate = translate;
            IsGlobal = translate.IsGlobal;
            ActionList = translate.Actions;
            VarCollection = translate.DeltinScript.VarCollection;

            IndexAssigner = translate.DeltinScript.DefaultIndexAssigner;
        }
        private ActionSet(ActionSet other)
        {
            Translate = other.Translate;
            IsGlobal = other.IsGlobal;
            ActionList = other.ActionList;
            VarCollection = other.VarCollection;

            IndexAssigner = other.IndexAssigner;
            ReturnHandler = other.ReturnHandler;
            CurrentObject = other.CurrentObject;
            CurrentObjectRelatedIndex = other.CurrentObjectRelatedIndex;
            This = other.This;
            IsRecursive = other.IsRecursive;
            CommentNext = other.CommentNext;
            ThisTypeLinker = other.ThisTypeLinker;
        }

        public ActionSet New(VarIndexAssigner indexAssigner) => new ActionSet(this)
        {
            IndexAssigner = indexAssigner ?? throw new ArgumentNullException(nameof(indexAssigner))
        };
        public ActionSet New(ReturnHandler returnHandler) => new ActionSet(this)
        {
            ReturnHandler = returnHandler ?? throw new ArgumentNullException(nameof(returnHandler))
        };
        public ActionSet New(IWorkshopTree currentObject) => new ActionSet(this) { CurrentObject = currentObject };
        public ActionSet New(IGettable relatedIndex, Element target = null) => new ActionSet(this) { CurrentObjectRelatedIndex = new SourceIndexReference(relatedIndex, target) };
        public ActionSet New(bool isRecursive) => new ActionSet(this) { IsRecursive = isRecursive };
        public ActionSet ContainVariableAssigner() => new ActionSet(this) { IndexAssigner = IndexAssigner.CreateContained() };
        public ActionSet PackThis() => new ActionSet(this) { This = CurrentObject };
        public ActionSet SetThis(IWorkshopTree value) => new ActionSet(this) { This = value };
        public ActionSet SetNextComment(string comment) => new ActionSet(this) { CommentNext = new ActionComment(comment) };
        public ActionSet SetThisTypeLinker(InstanceAnonymousTypeLinker thisTypeLinker) => new ActionSet(this) { ThisTypeLinker = thisTypeLinker };
        public ActionSet MergeTypeLinker(InstanceAnonymousTypeLinker thisTypeLinker)
        {
            // Do nothing if the type linker is null.
            if (thisTypeLinker == null) return this;

            // Clone the ActionSet.
            var clone = new ActionSet(this);
            
            // If there was no type linker to begin with, set the type linker to the one provided.
            if (clone.ThisTypeLinker == null)
                clone.ThisTypeLinker = thisTypeLinker;
            else // Otherwise, merge the existing type linker and the provided type linker.
                clone.ThisTypeLinker = clone.ThisTypeLinker.CloneMerge(thisTypeLinker);

            return clone;
        }

        public void AddAction(string comment, params IWorkshopTree[] actions)
        {
            foreach (var action in actions)
            {
                if (action is Element element && comment != null)
                {
                    element.Comment = comment;
                    comment = null;
                }
                AddAction(new ALAction(action));
            }
        }
        public void AddAction(params IWorkshopTree[] actions) => AddAction(null, actions);
        public void AddAction(params IActionList[] actions)
        {
            ActionList.AddRange(actions);
            if (CommentNext != null && !CommentNext.Used && actions[0] is ALAction alaction && alaction.Calling is Element element)
            {
                element.Comment = CommentNext.GetValue();
                CommentNext = null;
            }
        }

        public ActionSet InitialSet()
        {
            if (IsGlobal) return Translate.DeltinScript.InitialGlobal.ActionSet;
            else return Translate.DeltinScript.InitialPlayer.ActionSet;
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
        public string Comment { get; set; }
        public bool IsAction { get; } = true;

        public SkipStartMarker(ActionSet actionSet, IWorkshopTree condition, string comment = null)
        {
            ActionSet = actionSet;
            Condition = condition;
            Comment = comment;
        }
        public SkipStartMarker(ActionSet actionSet, string comment = null)
        {
            ActionSet = actionSet;
            Comment = comment;
        }

        public DynamicSkip GetSkipCount(SkipEndMarker marker) => new DynamicSkip(this, marker);

        public int NumberOfActionsToMarker(SkipEndMarker marker)
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

            return count - 1;
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
            IWorkshopTree skipCount;
            if (SkipCount != null) skipCount = SkipCount;
            else skipCount = GetSkipCount(EndMarker);

            Element newAction;
            if (Condition == null) newAction = Element.Part("Skip", skipCount);
            else newAction = Element.Part("Skip If", Element.Not(Condition), skipCount);

            newAction.Comment = Comment;
            return newAction;
        }

        public bool ShouldRemove() => NumberOfActionsToMarker(EndMarker) == 0;
    }

    public class DynamicSkip : IWorkshopTree
    {
        public SkipStartMarker StartMarker { get; set; }
        public SkipEndMarker EndMarker { get; set; }

        public DynamicSkip(SkipStartMarker startMarker, SkipEndMarker endMarker)
        {
            StartMarker = startMarker;
            EndMarker = endMarker;
        }

        public void ToWorkshop(WorkshopBuilder b, ToWorkshopContext context) => b.Append(StartMarker.NumberOfActionsToMarker(EndMarker).ToString());

        public bool EqualTo(IWorkshopTree other) => false;
    }

    public class SkipEndMarker : IActionList
    {
        public bool IsAction { get; } = false;
        public Element GetAction() => throw new NotImplementedException();
        public bool ShouldRemove() => false;
    }

    public struct SourceIndexReference
    {
        public IGettable Reference { get; }
        public Element Target { get; }

        public SourceIndexReference(IGettable reference, Element target)
        {
            Reference = reference;
            Target = target;
        }
    }

    public class ActionComment
    {
        private readonly string _value;
        public bool Used { get; private set; }
        public ActionComment(string value) => _value = value;
        public string GetValue()
        {
            Used = true;
            return _value;
        }
    }
}