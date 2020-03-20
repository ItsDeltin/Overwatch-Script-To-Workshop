using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    class MethodBuilder
    {
        public static IWorkshopTree Call(DefinedMethod method, MethodCall call, ActionSet callerSet)
        {
            // Subroutine call.
            if (method.IsSubroutine)
                return CallSubroutine(method, call, callerSet);
            // Inline
            else
            {
                MethodBuilder builder = new MethodBuilder(method, callerSet);

                // Inline-recursive method call.
                if (method.Attributes.Recursive)
                    return CallInlineRecursive(builder, method, call, callerSet);
                else
                {
                    if (method.virtualSubroutineAssigned != null)
                    {
                        // TODO: Maybe remove this?
                        // Virtual in another subroutine
                        return Call(method.virtualSubroutineAssigned, call, callerSet);
                    }
                    else
                    {
                        // Normal
                        builder.AssignParameters(call);
                        builder.SetupReturnHandler();
                        builder.ParseInner();
                        builder.ReturnHandler.ApplyReturnSkips();
                        return builder.ReturnHandler.GetReturnedValue();
                    }
                }
            }
        }

        private static IWorkshopTree CallInlineRecursive(MethodBuilder builder, DefinedMethod method, MethodCall call, ActionSet callerSet)
        {
            RecursiveStack lastCall = RecursiveStack.GetRecursiveCall(callerSet.Translate.MethodStack, method);

            if (lastCall == null)
            {
                RecursiveStack parser = new RecursiveStack(builder);
                callerSet.Translate.MethodStack.Add(parser);
                parser.ParseCall(call);
                callerSet.Translate.MethodStack.Remove(parser);

                return builder.ReturnHandler.GetReturnedValue();
            }
            else
            {
                lastCall.RecursiveCall(call, callerSet);
                return lastCall.Builder.ReturnHandler.GetReturnedValue();
            }
        }

        private static IWorkshopTree CallSubroutine(DefinedMethod method, MethodCall call, ActionSet callerSet)
        {
            method.SetupSubroutine();

            for (int i = 0; i < method.subroutineInfo.ParameterStores.Length; i++)
            {
                // Normal parameter push.
                if (!method.Attributes.Recursive)
                    callerSet.AddAction(method.subroutineInfo.ParameterStores[i].SetVariable((Element)call.ParameterValues[i]));
                // Recursive parameter push.
                else
                    callerSet.AddAction(((RecursiveIndexReference)method.subroutineInfo.ParameterStores[i]).Push((Element)call.ParameterValues[i]));
            }

            // Store the object the subroutine is executing with.
            if (method.subroutineInfo.ObjectStore != null)
            {
                // Normal
                if (!method.Attributes.Recursive)
                    callerSet.AddAction(method.subroutineInfo.ObjectStore.SetVariable((Element)callerSet.CurrentObject));
                // Recursive: Stack
                else
                    callerSet.AddAction(method.subroutineInfo.ObjectStore.ModifyVariable(Operation.AppendToArray, (Element)callerSet.CurrentObject));
            }

            switch (call.CallParallel)
            {
                // No parallel, call subroutine normally.
                case CallParallel.NoParallel:
                    callerSet.AddAction(Element.Part<A_CallSubroutine>(method.subroutineInfo.Subroutine));
                    return method.subroutineInfo.ReturnHandler.GetReturnedValue();
                
                // Restart the subroutine if it is already running.
                case CallParallel.AlreadyRunning_RestartRule:
                    callerSet.AddAction(Element.Part<A_StartRule>(method.subroutineInfo.Subroutine, EnumData.GetEnumValue(IfAlreadyExecuting.RestartRule)));
                    return null;
                
                // Do nothing if the subroutine is already running.
                case CallParallel.AlreadyRunning_DoNothing:
                    callerSet.AddAction(Element.Part<A_StartRule>(method.subroutineInfo.Subroutine, EnumData.GetEnumValue(IfAlreadyExecuting.DoNothing)));
                    return null;
                
                default: throw new NotImplementedException();
            }
        }

        public readonly DefinedMethod Method;
        public ActionSet BuilderSet { get; set; }
        public ReturnHandler ReturnHandler { get; private set; }

        public MethodBuilder(DefinedMethod method, ActionSet builderSet, ReturnHandler returnHandler = null)
        {
            Method = method;
            this.BuilderSet = builderSet;
            ReturnHandler = returnHandler;
        }

        public void AssignParameters(MethodCall methodCall)
        {
            DefinedMethod.AssignParameters(BuilderSet, Method.ParameterVars, methodCall.ParameterValues);
        }

        public void SetupReturnHandler()
        {
            if (Method.DoesReturnValue() && ReturnHandler == null)
            {
                ReturnHandler = new ReturnHandler(BuilderSet, Method.Name, Method.Attributes.Virtual || Method.multiplePaths);
                BuilderSet = BuilderSet.New(ReturnHandler);
            }
        }

        public void ParseInner()
        {
            if (Method.Attributes.WasOverriden) ParseVirtual();
            else TranslateSegment(BuilderSet, Method);
        }

        private void ParseVirtual()
        {
            // Loop through all potential methods.
            DefinedMethod[] options = Array.ConvertAll(Method.Attributes.AllOverrideOptions(), iMethod => (DefinedMethod)iMethod);

            // Create the switch that chooses the overload.
            SwitchBuilder typeSwitch = new SwitchBuilder(BuilderSet);

            // Parse the current overload.
            typeSwitch.NextCase(new V_Number(((ClassType)Method.Attributes.ContainingType).Identifier));
            TranslateSegment(BuilderSet, Method);

            foreach (DefinedMethod option in options)
            {
                // The action set for the overload.
                ActionSet optionSet = BuilderSet.New(BuilderSet.IndexAssigner.CreateContained());

                // Add the object variables of the selected method.
                option.Attributes.ContainingType.AddObjectVariablesToAssigner(optionSet.CurrentObject, optionSet.IndexAssigner);

                // Go to next case then parse the block.
                typeSwitch.NextCase(new V_Number(((ClassType)option.Attributes.ContainingType).Identifier));
                TranslateSegment(optionSet, option);

                if (Method.IsSubroutine) option.virtualSubroutineAssigned = Method;
            }

            ClassData classData = BuilderSet.Translate.DeltinScript.SetupClasses();

            // Finish the switch.
            typeSwitch.Finish(Element.Part<V_ValueInArray>(classData.ClassIndexes.GetVariable(), BuilderSet.CurrentObject));
        }

        private static void TranslateSegment(ActionSet actionSet, DefinedMethod method)
        {
            method.block.Translate(actionSet);
        }
    }
}