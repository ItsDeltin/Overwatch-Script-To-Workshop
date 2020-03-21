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

        public RecursiveStack(MethodBuilder builder) : base(builder.Method)
        {
            this.Builder = builder;
        }

        /// <summary>Calls the method.</summary>
        public void ParseCall(MethodCall call)
        {
            // Create the array used for continuing after a recursive call.
            continueArray = Builder.BuilderSet.VarCollection.Assign("_" + Builder.Method.Name + "_recursiveContinue", Builder.BuilderSet.IsGlobal, false);
            nextContinue = Builder.BuilderSet.VarCollection.Assign("_" + Builder.Method.Name + "_nextContinue", Builder.BuilderSet.IsGlobal, true);
            Builder.BuilderSet.InitialSet().AddAction(continueArray.SetVariable(new V_EmptyArray()));
            
            if (Builder.Method.Attributes.Virtual)
            {
                objectStore = Builder.BuilderSet.VarCollection.Assign("_" + Builder.Method.Name + "_objectStack", Builder.BuilderSet.IsGlobal, false);
                Builder.BuilderSet.AddAction(objectStore.SetVariable(Element.CreateArray(Builder.BuilderSet.CurrentObject)));
                Builder.BuilderSet = Builder.BuilderSet.New(Element.Part<V_LastOf>(objectStore.GetVariable()));
            }

            // Assign the parameters.
            Builder.Method.AssignParameters(Builder.BuilderSet, call.ParameterValues, true);

            Builder.SetupReturnHandler();

            // Create the recursive loop.
            Builder.BuilderSet.AddAction(Element.Part<A_While>(new V_True()));

            // Create the continue skip action.
            continueAt = new SkipStartMarker(Builder.BuilderSet);
            continueAt.SetSkipCount((Element)nextContinue.GetVariable());
            Builder.BuilderSet.AddAction(continueAt);

            // Translate the method's block.
            Builder.ParseInner();

            Builder.BuilderSet.ReturnHandler.ApplyReturnSkips();

            Builder.BuilderSet.AddAction(objectStore.ModifyVariable(Operation.RemoveFromArrayByIndex, Element.Part<V_CountOf>(objectStore.GetVariable()) - 1));

            // Pop the parameters.
            PopParameterStacks();

            // Restart the method from the specified position if there are any elements in the continue array.
            Builder.BuilderSet.AddAction(Element.Part<A_SkipIf>(new V_Compare(
                Element.Part<V_CountOf>(continueArray.GetVariable()),
                Operators.Equal,
                new V_Number(0)
            ), new V_Number(3)));

            // Store the next continue and pop the continue array.
            Builder.BuilderSet.AddAction(nextContinue.SetVariable(Element.Part<V_LastOf>(continueArray.GetVariable())));
            Builder.BuilderSet.AddAction(continueArray.ModifyVariable(Operation.RemoveFromArrayByIndex, Element.Part<V_CountOf>(continueArray.GetVariable()) - 1));

            // Mark the end of the method.
            Builder.BuilderSet.AddAction(endOfMethod);
            Builder.BuilderSet.AddAction(new A_End());

            // Reset nextContinue.
            Builder.BuilderSet.AddAction(nextContinue.SetVariable(0));
        }

        /// <summary>The method was already called in the stack.</summary>
        public void RecursiveCall(MethodCall methodCall, ActionSet callerSet)
        {
            // Push object array.
            if (Builder.Method.Attributes.Virtual)
                Builder.BuilderSet.AddAction(objectStore.ModifyVariable(Operation.AppendToArray, (Element)callerSet.CurrentObject));

            // Push new parameters.
            for (int i = 0; i < Builder.Method.ParameterVars.Length; i++)
            {
                var varReference = Builder.BuilderSet.IndexAssigner[Builder.Method.ParameterVars[i]];
                if (varReference is RecursiveIndexReference)
                {
                    Builder.BuilderSet.AddAction(((RecursiveIndexReference)varReference).Push(
                        (Element)methodCall.ParameterValues[i]
                    ));
                }
            }

            // Add to the continue skip array.
            V_Number skipLength = new V_Number();
            Builder.BuilderSet.AddAction(continueArray.ModifyVariable(
                Operation.AppendToArray,
                skipLength
            ));

            // Restart the method.
            SkipStartMarker resetSkip = new SkipStartMarker(Builder.BuilderSet);
            resetSkip.SetEndMarker(endOfMethod);
            Builder.BuilderSet.AddAction(resetSkip);

            SkipEndMarker continueAtMarker = new SkipEndMarker();
            Builder.BuilderSet.AddAction(continueAtMarker);
            skipLength.Value = continueAt.NumberOfActionsToMarker(continueAtMarker);
        }

        private void PopParameterStacks()
        {
            PopParameterStacks(Builder.BuilderSet, Builder.Method.ParameterVars);
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