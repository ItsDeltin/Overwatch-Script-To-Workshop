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
        public TranslateRule Translate { get; }
        public DocRange GenericErrorRange { get; }
        public VarIndexAssigner IndexAssigner { get; }
        public FileDiagnostics Diagnostics => Translate.Script.Diagnostics;
        public bool IsGlobal               => Translate.IsGlobal;
        private List<IActionList> actions  => Translate.Actions;
        public VarCollection VarCollection => Translate.DeltinScript.VarCollection;

        public ActionSet(TranslateRule translate, DocRange genericErrorRange, List<IActionList> actions)
        {
            Translate = translate;
            GenericErrorRange = genericErrorRange;
            IndexAssigner = translate.DeltinScript.DefaultIndexAssigner;
        }
        private ActionSet(ActionSet actionSet, DocRange genericErrorRange)
        {
            Translate = actionSet.Translate;
            GenericErrorRange = genericErrorRange;
            IndexAssigner = Translate.DeltinScript.DefaultIndexAssigner;
        }
        private ActionSet(ActionSet actionSet, VarIndexAssigner assigner)
        {
            Translate = actionSet.Translate;
            GenericErrorRange = actionSet.GenericErrorRange;
            IndexAssigner = assigner;
        }

        public ActionSet New(DocRange range)
        {
            return new ActionSet(this, range);
        }
        public ActionSet New(VarIndexAssigner indexAssigner)
        {
            return new ActionSet(this, indexAssigner);
        }

        public void AddAction(IWorkshopTree action)
        {
            actions.Add(new ALAction(action));
        }
        public void AddAction(IWorkshopTree[] actions)
        {
            foreach (var action in actions)
                this.actions.Add(new ALAction(action));
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
        public IWorkshopTree Calling { get; }

        public ALAction(IWorkshopTree calling)
        {
            Calling = calling;
        }
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