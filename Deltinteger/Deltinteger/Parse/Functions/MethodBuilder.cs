using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class MethodBuilder
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
                        builder.BuilderSet = builder.BuilderSet.PackThis();
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

        public DefinedMethod Method { get; }
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
            Method.AssignParameters(BuilderSet, methodCall.ParameterValues, false);
        }

        public void SetupReturnHandler()
        {
            if (ReturnHandler == null)
            {
                ReturnHandler = new ReturnHandler(BuilderSet, Method.Name, (Method.Attributes.Virtual && Method.ReturnType != null) || Method.multiplePaths);
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
            if (Method.Attributes.Virtual)
                typeSwitch.AddDefault();
            else
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

                // Iterate through every type.
                foreach (CodeType type in BuilderSet.Translate.DeltinScript.Types.AllTypes)
                    // If 'type' does not equal the current virtual option's containing class...
                    if (option.Attributes.ContainingType != type
                        // ...and 'type' implements the containing class...
                        && type.Implements(option.Attributes.ContainingType)
                        // ...and 'type' does not have their own function implementation...
                        && AutoImplemented(option.Attributes.ContainingType, options.Select(o => o.Attributes.ContainingType).ToArray(), type))
                        // ...then add an additional case for 'type's class identifier.
                        typeSwitch.NextCase(new V_Number(((ClassType)type).Identifier));

                if (option.subroutineInfo == null)
                    TranslateSegment(optionSet, option);
                else
                {
                    option.SetupSubroutine();
                    BuilderSet.AddAction(Element.Part<A_StartRule>(option.subroutineInfo.Subroutine, EnumData.GetEnumValue(IfAlreadyExecuting.DoNothing)));
                    if (Method.ReturnType != null) ReturnHandler.ReturnValue(option.subroutineInfo.ReturnHandler.GetReturnedValue());
                }

                if (Method.IsSubroutine) option.virtualSubroutineAssigned = Method;
            }

            ClassData classData = BuilderSet.Translate.DeltinScript.GetComponent<ClassData>();

            // Finish the switch.
            typeSwitch.Finish(Element.Part<V_ValueInArray>(classData.ClassIndexes.GetVariable(), BuilderSet.CurrentObject));
        }

        /// <summary>Determines if the specified type does not have their own implementation for the specified virtual function.</summary>
        /// <param name="virtualFunction">The virtual function to check overrides of.</param>
        /// <param name="options">All potential virtual functions.</param>
        /// <param name="type">The type to check.</param>
        public static bool AutoImplemented(CodeType virtualType, CodeType[] allOptionTypes, CodeType type)
        {
            // Go through each class in the inheritance tree and check if it implements the function.
            CodeType current = type;
            while (current != null && current != virtualType)
            {
                // If it does, return false.
                if (allOptionTypes.Contains(current)) return false;
                current = current.Extends;
            }
            return true;
        }

        private static void TranslateSegment(ActionSet actionSet, DefinedMethod method)
        {
            method.block.Translate(actionSet);
        }
    }
}