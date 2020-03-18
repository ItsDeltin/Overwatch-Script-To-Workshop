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

        /// <summary>The skip used to return the executing position after a recursive call.</summary>
        private SkipStartMarker continueAt;
        
        /// <summary>Marks the end of the method.</summary>
        private readonly SkipEndMarker endOfMethod = new SkipEndMarker();

        /// <summary>The recursive method data.</summary>
        protected readonly DefinedMethod method;

        /// <summary>The return handler.</summary>
        protected ReturnHandler returnHandler;

        public RecursiveStack(DefinedMethod method) : base((IApplyBlock)method)
        {
            this.method = method;
        }

        /// <summary>Calls the method.</summary>
        private IWorkshopTree ParseCall(ActionSet actionSet, MethodCall methodCall)
        {
            // Create the array used for continuing after a recursive call.
            continueArray = actionSet.VarCollection.Assign("_" + method.Name + "_recursiveContinue", actionSet.IsGlobal, false);
            nextContinue = actionSet.VarCollection.Assign("_" + method.Name + "_nextContinue", actionSet.IsGlobal, true);
            actionSet.InitialSet().AddAction(continueArray.SetVariable(new V_EmptyArray()));

            ReturnHandler returnHandler = methodCall.ReturnHandler ?? new ReturnHandler(actionSet, method.Name, method.multiplePaths);
            this.returnHandler = returnHandler;

            DefinedMethod.AssignParameters(actionSet, method.ParameterVars, methodCall.ParameterValues, true);

            actionSet.AddAction(Element.Part<A_While>(new V_True()));

            continueAt = new SkipStartMarker(actionSet);
            continueAt.SetSkipCount((Element)nextContinue.GetVariable());
            actionSet.AddAction(continueAt);

            method.block.Translate(actionSet.New(returnHandler));

            PopParameterStacks(actionSet);

            // Pop the continueArray.
            actionSet.AddAction(Element.Part<A_SkipIf>(new V_Compare(
                Element.Part<V_CountOf>(continueArray.GetVariable()),
                Operators.Equal,
                new V_Number(0)
            ), new V_Number(3)));

            actionSet.AddAction(nextContinue.SetVariable(Element.Part<V_LastOf>(continueArray.GetVariable())));
            actionSet.AddAction(continueArray.ModifyVariable(Operation.RemoveFromArrayByIndex, Element.Part<V_CountOf>(continueArray.GetVariable()) - 1));

            actionSet.AddAction(endOfMethod);
            actionSet.AddAction(new A_End());

            actionSet.AddAction(nextContinue.SetVariable(0));

            return returnHandler.GetReturnedValue();
        }

        /// <summary>The method was already called in the stack.</summary>
        private IWorkshopTree RecursiveCall(ActionSet actionSet, MethodCall methodCall)
        {
            // Push new parameters.
            for (int i = 0; i < method.ParameterVars.Length; i++)
            {
                var varReference = actionSet.IndexAssigner[method.ParameterVars[i]];
                if (varReference is RecursiveIndexReference)
                {
                    actionSet.AddAction(((RecursiveIndexReference)varReference).Push(
                        (Element)methodCall.ParameterValues[i]
                    ));
                }
            }

            // Add to the continue skip array.
            V_Number skipLength = new V_Number();
            actionSet.AddAction(continueArray.ModifyVariable(
                Operation.AppendToArray,
                skipLength
            ));

            // Restart the method.
            SkipStartMarker resetSkip = new SkipStartMarker(actionSet);
            resetSkip.SetEndMarker(endOfMethod);
            actionSet.AddAction(resetSkip);

            SkipEndMarker continueAtMarker = new SkipEndMarker();
            actionSet.AddAction(continueAtMarker);
            skipLength.Value = continueAt.NumberOfActionsToMarker(continueAtMarker);

            return returnHandler.GetReturnedValue();
        }

        private void PopParameterStacks(ActionSet actionSet)
        {
            for (int i = 0; i < method.ParameterVars.Length; i++)
            {
                var pop = (actionSet.IndexAssigner[method.ParameterVars[i]] as RecursiveIndexReference)?.Pop();
                if (pop != null) actionSet.AddAction(pop);
            }
        }

        public static IWorkshopTree Call(DefinedMethod method, MethodCall call, ActionSet actionSet)
        {
            RecursiveStack lastCall = GetRecursiveCall(actionSet.Translate.MethodStack, method);

            if (lastCall == null)
            {
                RecursiveStack parser = new RecursiveStack(method);

                actionSet.Translate.MethodStack.Add(parser);
                var result = parser.ParseCall(actionSet, call);
                actionSet.Translate.MethodStack.Remove(parser);

                return result;
            }
            else
            {
                return lastCall.RecursiveCall(actionSet, call);
            }
        }

        private static RecursiveStack GetRecursiveCall(List<MethodStack> stack, DefinedMethod method)
            => stack.FirstOrDefault(ms => ms is RecursiveStack && ms.Function is DefinedMethod dm && dm.Attributes.AllOverrideOptions().Contains(method)) as RecursiveStack;
    }
}