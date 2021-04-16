using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    public class RecursiveStack
    {
        public object Identifier { get; }

        readonly WorkshopFunctionBuilder _builder;

        /// <summary>Marks the end of the method.</summary>
        readonly SkipEndMarker _endOfMethod = new SkipEndMarker();

        /// <summary>An array of positions to return to after a recursive call.</summary>
        IndexReference _continueArray;

        /// <summary>The next spot to continue at.</summary>
        IndexReference _nextContinue;

        /// <summary>Stores the current object.</summary>
        IndexReference _objectStore;

        /// <summary>The skip used to return the executing position after a recursive call.</summary>
        SkipStartMarker _continueAt;

        // Properties
        ActionSet actionSet => _builder.ActionSet;
        VarCollection varCollection => actionSet.VarCollection;
        bool isGlobal => actionSet.IsGlobal;
        string name => "func";

        public RecursiveStack(WorkshopFunctionBuilder builder, object identifier)
        {
            Identifier = identifier;
            _builder = builder;
        }

        public void Init()
        {
            // Create the array used for continuing after a recursive call.
            _continueArray = varCollection.Assign("_" + name + "_recursiveContinue", isGlobal, false);
            _nextContinue = varCollection.Assign("_" + name + "_nextContinue", isGlobal, true);
            actionSet.InitialSet().AddAction(_continueArray.SetVariable(Element.EmptyArray()));
            
            if (_builder.Controller.Attributes.RecursiveRequiresObjectStack)
            {
                _objectStore = varCollection.Assign("_" + name + "_objectStack", isGlobal, false);
                actionSet.AddAction(_objectStore.SetVariable(Element.CreateArray(actionSet.CurrentObject)));

                _builder.ModifySet(actionSet => actionSet.New(Element.LastOf(_objectStore.GetVariable())).PackThis());
            }
            _builder.ModifySet(actionSet => actionSet.New(true));
        }

        public void StartRecursiveLoop()
        {
            // Create the recursive loop.
            actionSet.AddAction(Element.While(Element.True()));

            // Create the continue skip action.
            _continueAt = new SkipStartMarker(actionSet);
            _continueAt.SetSkipCount((Element)_nextContinue.GetVariable());
            actionSet.AddAction(_continueAt);
        }

        public void EndRecursiveLoop()
        {
            // Pop the object store array.
            if (_builder.Controller.Attributes.IsInstance)
                actionSet.AddAction(_objectStore.ModifyVariable(Operation.RemoveFromArrayByIndex, Element.CountOf(_objectStore.GetVariable()) - 1));
            
            // Pop the parameters.
            _builder.ParameterHandler.Pop(_builder.ActionSet);

            // Restart the method from the specified position if there are any elements in the continue array.
            actionSet.AddAction(Element.SkipIf(Element.Compare(
                Element.CountOf(_continueArray.GetVariable()),
                Operator.Equal,
                Element.Num(0)
            ), Element.Num(3)));

            // Store the next continue and pop the continue array.
            actionSet.AddAction(_nextContinue.SetVariable(Element.LastOf(_continueArray.GetVariable())));
            actionSet.AddAction(_continueArray.ModifyVariable(Operation.RemoveFromArrayByIndex, Element.CountOf(_continueArray.GetVariable()) - 1));

            // Mark the end of the method.
            actionSet.AddAction(_endOfMethod);
            actionSet.AddAction(Element.End());

            // Reset nextContinue.
            actionSet.AddAction(_nextContinue.SetVariable(0));
        }

        /// <summary>The method was already called in the stack.</summary>
        public void RecursiveCall(ICallInfo callInfo, ActionSet callerSet)
        {
            // Push object array.
            if (_builder.Controller.Attributes.RecursiveRequiresObjectStack)
                actionSet.AddAction(_objectStore.ModifyVariable(Operation.AppendToArray, (Element)callerSet.CurrentObject));

            // Push new parameters.
            _builder.ParameterHandler.Push(_builder.ActionSet, callInfo.ParameterValues);

            // Add to the continue skip array.
            var skipLength = new NumberElement();
            actionSet.AddAction(_continueArray.ModifyVariable(
                Operation.AppendToArray,
                skipLength
            ));

            // Restart the method.
            SkipStartMarker resetSkip = new SkipStartMarker(actionSet);
            resetSkip.SetEndMarker(_endOfMethod);
            actionSet.AddAction(resetSkip);

            SkipEndMarker continueAtMarker = new SkipEndMarker();
            actionSet.AddAction(continueAtMarker);
            skipLength.Value = _continueAt.NumberOfActionsToMarker(continueAtMarker);
        }
    }
}