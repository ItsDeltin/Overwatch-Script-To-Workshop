using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class TranslateRule
    {
        private readonly List<IActionList> Actions = new List<IActionList>();
        public DeltinScript DeltinScript { get; }
        public ScriptFile Script { get; }
        private RuleAction RuleAction { get; }
        public bool IsGlobal { get; }

        public TranslateRule(ScriptFile script, DeltinScript deltinScript, RuleAction ruleAction)
        {
            DeltinScript = deltinScript;
            Script = script;
            RuleAction = ruleAction;
            IsGlobal = RuleAction.EventType == RuleEvent.OngoingGlobal;

            var actionSet = new ActionSet(this, null, Actions);
            ruleAction.Block.Translate(actionSet);
        }
    }

    public class ActionSet
    {
        public bool IsGlobal { get; }
        public TranslateRule Translate { get; }
        public FileDiagnostics Diagnostics { get; }
        public DocRange GenericErrorRange { get; }
        private readonly List<IActionList> actions;

        public ActionSet(TranslateRule translate, DocRange genericErrorRange, List<IActionList> actions)
        {
            this.actions = actions;
            Translate = translate;
            GenericErrorRange = genericErrorRange;
            Diagnostics = Translate.Script.Diagnostics;
            IsGlobal = translate.IsGlobal;
        }
        private ActionSet(ActionSet actionSet, DocRange genericErrorRange, List<IActionList> actions)
        {
            this.actions = actions;
            Translate = actionSet.Translate;
            Diagnostics = actionSet.Diagnostics;
            GenericErrorRange = genericErrorRange;
        }

        public ActionSet New(DocRange range)
        {
            return new ActionSet(this, range, actions);
        }

        public void AddAction(Element action)
        {
            actions.Add((ALAction)action);
        }
        public void AddAction(IActionList action)
        {
            actions.Add(action);
        }
        public void AddAction(IActionList[] actions)
        {
            this.actions.AddRange(actions);
        }
    }

    public interface IActionList
    {
    }

    public class ALAction : IActionList
    {
        public Element Calling { get; }

        public ALAction(Element calling)
        {
            Calling = calling;
        }

        public static implicit operator ALAction(Element element) => new ALAction(element);
    }

    public class ALSkip : IActionList
    {
        public ALSkipMarker SkipTo { get; }
        public Element Condition { get; }

        public ALSkip(ALSkipMarker skipTo, Element condition)
        {
            SkipTo = skipTo;
            Condition = condition;
        }
        public ALSkip(ALSkipMarker skipTo)
        {
            SkipTo = skipTo;
        }

        public Element GetSkip()
        {
            // todo: this
            throw new NotImplementedException();
        }
    }

    public class ALSkipMarker : IActionList {}
}