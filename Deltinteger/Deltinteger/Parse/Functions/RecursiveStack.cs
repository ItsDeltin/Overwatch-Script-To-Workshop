using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    class RecursiveStack : MethodStack
    {
        /// <summary>An array of positions to return to after a recursive call.</summary>
        private IndexReference continueArray;

        /// <summary>The next spot to continue at.</summary>
        private IndexReference nextContinue;

        /// <summary>Stores the current object.</summary>
        private IndexReference objectStore;

        /// <summary>The skip used to return the executing position after a recursive call.</summary>
        private SkipStartMarker continueAt;
        
        /// <summary>Marks the end of the method.</summary>
        private readonly SkipEndMarker endOfMethod = new SkipEndMarker();

        /// <summary>The recursive method data.</summary>
        public readonly MethodBuilder Builder;

        private ActionSet ActionSet
        {
            get => Builder.BuilderSet;
            set
            {
                Builder.BuilderSet = value;
            }
        }

        public RecursiveStack(MethodBuilder builder) : base(builder.Method)
        {
            this.Builder = builder;
        }

        /// <summary>Calls the method.</summary>
        public void ParseCall(MethodCall call)
        {
            // Create the array used for continuing after a recursive call.
            continueArray = ActionSet.VarCollection.Assign("_" + Builder.Method.Name + "_recursiveContinue", ActionSet.IsGlobal, false);
            nextContinue = ActionSet.VarCollection.Assign("_" + Builder.Method.Name + "_nextContinue", ActionSet.IsGlobal, true);
            ActionSet.InitialSet().AddAction(continueArray.SetVariable(new V_EmptyArray()));
            
            if (Builder.Method.Attributes.Virtual)
            {
                objectStore = ActionSet.VarCollection.Assign("_" + Builder.Method.Name + "_objectStack", ActionSet.IsGlobal, false);
                ActionSet.AddAction(objectStore.SetVariable(Element.CreateArray(ActionSet.CurrentObject)));
                ActionSet = ActionSet.New(Element.Part<V_LastOf>(objectStore.GetVariable()));
            }
            ActionSet = ActionSet.New(true);

            // Assign the parameters.
            Builder.Method.AssignParameters(ActionSet, call.ParameterValues, true);

            Builder.SetupReturnHandler();

            // Create the recursive loop.
            ActionSet.AddAction(Element.Part<A_While>(new V_True()));

            // Create the continue skip action.
            continueAt = new SkipStartMarker(ActionSet);
            continueAt.SetSkipCount((Element)nextContinue.GetVariable());
            ActionSet.AddAction(continueAt);

            // Translate the method's block.
            Builder.ParseInner();

            ActionSet.ReturnHandler.ApplyReturnSkips();

            // Pop the object store array.
            if (Builder.Method.Attributes.Virtual)
                ActionSet.AddAction(objectStore.ModifyVariable(Operation.RemoveFromArrayByIndex, Element.Part<V_CountOf>(objectStore.GetVariable()) - 1));
            
            // Pop the parameters.
            PopParameterStacks();

            // Restart the method from the specified position if there are any elements in the continue array.
            ActionSet.AddAction(Element.Part<A_SkipIf>(new V_Compare(
                Element.Part<V_CountOf>(continueArray.GetVariable()),
                Operators.Equal,
                new V_Number(0)
            ), new V_Number(3)));

            // Store the next continue and pop the continue array.
            ActionSet.AddAction(nextContinue.SetVariable(Element.Part<V_LastOf>(continueArray.GetVariable())));
            ActionSet.AddAction(continueArray.ModifyVariable(Operation.RemoveFromArrayByIndex, Element.Part<V_CountOf>(continueArray.GetVariable()) - 1));

            // Mark the end of the method.
            ActionSet.AddAction(endOfMethod);
            ActionSet.AddAction(new A_End());

            // Reset nextContinue.
            ActionSet.AddAction(nextContinue.SetVariable(0));
        }

        /// <summary>The method was already called in the stack.</summary>
        public void RecursiveCall(MethodCall methodCall, ActionSet callerSet)
        {
            // Push object array.
            if (Builder.Method.Attributes.Virtual)
                ActionSet.AddAction(objectStore.ModifyVariable(Operation.AppendToArray, (Element)callerSet.CurrentObject));

            // Push new parameters.
            for (int i = 0; i < Builder.Method.ParameterVars.Length; i++)
            {
                var varReference = ActionSet.IndexAssigner[Builder.Method.ParameterVars[i]];
                if (varReference is RecursiveIndexReference)
                {
                    ActionSet.AddAction(((RecursiveIndexReference)varReference).Push(
                        (Element)methodCall.ParameterValues[i]
                    ));
                }
            }

            // Add to the continue skip array.
            V_Number skipLength = new V_Number();
            ActionSet.AddAction(continueArray.ModifyVariable(
                Operation.AppendToArray,
                skipLength
            ));

            // Restart the method.
            SkipStartMarker resetSkip = new SkipStartMarker(ActionSet);
            resetSkip.SetEndMarker(endOfMethod);
            ActionSet.AddAction(resetSkip);

            SkipEndMarker continueAtMarker = new SkipEndMarker();
            ActionSet.AddAction(continueAtMarker);
            skipLength.Value = continueAt.NumberOfActionsToMarker(continueAtMarker);
        }

        private void PopParameterStacks()
        {
            PopParameterStacks(ActionSet, Builder.Method.ParameterVars);
        }

        public static void PopParameterStacks(ActionSet actionSet, Var[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                var pop = (actionSet.IndexAssigner[parameters[i]] as RecursiveIndexReference)?.Pop();
                if (pop != null) actionSet.AddAction(pop);
            }
        }

        public static RecursiveStack GetRecursiveCall(List<MethodStack> stack, DefinedMethod method)
            => stack.FirstOrDefault(ms => ms is RecursiveStack && ms.Function is DefinedMethod dm && (method == dm || dm.Attributes.AllOverrideOptions().Contains(method))) as RecursiveStack;
    }
}