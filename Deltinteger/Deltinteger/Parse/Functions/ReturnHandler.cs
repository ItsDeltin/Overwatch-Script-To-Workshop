using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public class ReturnHandler
    {
        public List<RecursiveIndexReference> AdditionalPopOnReturn { get; } = new List<RecursiveIndexReference>();

        protected readonly ActionSet _actionSet;
        private readonly bool _multiplePaths;

        // If `MultiplePaths` is true, use `ReturnStore`. Else use `ReturningValue`.
        private readonly IndexReference _returnStore;
        private IWorkshopTree _returningValue;
        private bool _valueWasReturned;
        private readonly List<SkipStartMarker> _skips = new List<SkipStartMarker>();

        public ReturnHandler(ActionSet actionSet, string methodName, bool multiplePaths)
        {
            _actionSet = actionSet;
            _multiplePaths = multiplePaths;

            if (multiplePaths)
                _returnStore = actionSet.VarCollection.Assign("_" + methodName + "ReturnValue", actionSet.IsGlobal, true);
        }

        public virtual void ReturnValue(IWorkshopTree value)
        {
            if (!_multiplePaths && _valueWasReturned)
                throw new Exception("_multiplePaths is set as false and 2 expressions were returned.");
            _valueWasReturned = true;

            // Multiple return paths.
            if (_multiplePaths)
                _actionSet.AddAction(_returnStore.SetVariable((Element)value));
            // One return path.
            else
                _returningValue = value;
        }

        public virtual void Return(Scope returningFromScope, ActionSet returningSet)
        {
            if (returningSet.IsRecursive)
            {
                returningFromScope.EndScope(returningSet, true);

                foreach (var recursiveIndexReference in AdditionalPopOnReturn)
                    returningSet.AddAction(recursiveIndexReference.Pop());
            }

            SkipStartMarker returnSkipStart = new SkipStartMarker(returningSet);
            returningSet.AddAction(returnSkipStart);
            _skips.Add(returnSkipStart);
        }

        public virtual void ApplyReturnSkips()
        {
            SkipEndMarker methodEndMarker = new SkipEndMarker();
            _actionSet.AddAction(methodEndMarker);

            foreach (var returnSkip in _skips)
                returnSkip.SetEndMarker(methodEndMarker);
        }

        public virtual IWorkshopTree GetReturnedValue()
        {
            if (_multiplePaths)
                return _returnStore.GetVariable();
            else
                return _returningValue;
        }
    }

    public class RuleReturnHandler : ReturnHandler
    {
        public RuleReturnHandler(ActionSet actionSet) : base(actionSet, null, false) {}

        public override void ApplyReturnSkips() => throw new Exception("Can't apply return skips in a rule.");
        public override IWorkshopTree GetReturnedValue() => throw new Exception("Can't get the returned value of a rule.");
        public override void ReturnValue(IWorkshopTree value) => throw new Exception("Can't return a value in a rule.");

        public override void Return(Scope returningFromScope, ActionSet returningSet)
        {
            _actionSet.AddAction(Element.Part<A_Abort>());
        }
    }

    public interface IParseReturnHandler
    {
        void Validate(DocRange range, IExpression value);
    }

    public class ParseReturnHandler : IParseReturnHandler
    {
        public CodeType ExpectedType { get; }
        public bool AllowMultiple { get; }
        public bool MustReturnValue { get; }
        private readonly ParseInfo _parseInfo;
        private bool _returnFound;
        // Errors
        public string MustReturnValueMessage { get; set; } = "Must return a value.";
        public string MoreThanOneReturnMessage { get; set; } = "Cannot have more than one return statement if the function's return type is constant.";
        public string VoidReturnValueMessage { get; set; }

        public ParseReturnHandler(ParseInfo parseInfo)
        {
            _parseInfo = parseInfo;
        }

        public ParseReturnHandler(ParseInfo parseInfo, string objectName) : this(parseInfo)
        {
            VoidReturnValueMessage = objectName + " is void, so no value can be returned.";
        }

        public void Validate(DocRange range, IExpression value)
        {
            // Error if a value must be returned.
            if (MustReturnValue && value == null)
            {
                _parseInfo.Script.Diagnostics.Error(MustReturnValueMessage, range);
                return;
            }

            // Multiple return statements when not allowed.
            if (!AllowMultiple && _returnFound)
            {
                _parseInfo.Script.Diagnostics.Error(MoreThanOneReturnMessage, range);
                return;
            }
            _returnFound = true;
        }
    }
}