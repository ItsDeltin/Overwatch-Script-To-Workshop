using System;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    public class WorkshopFunctionBuilder
    {
        // Creates and calls a WorkshopFunctionBuilder.
        public static IWorkshopTree Call(ActionSet actionSet, ICallInfo call, IWorkshopFunctionController controller)
        {
            var builder = new WorkshopFunctionBuilder(actionSet, controller);
            return builder.Call(call);
        }

        public ActionSet ActionSet { get; private set; }
        public IWorkshopFunctionController Controller { get; }
        public IParameterHandler ParameterHandler { get; private set; }
        public ReturnHandler ReturnHandler { get; private set; }
        SubroutineCatalogItem _subroutine;

        public WorkshopFunctionBuilder(ActionSet actionSet, IWorkshopFunctionController controller)
        {
            ActionSet = actionSet;
            Controller = controller;
        }

        public IWorkshopTree Call(ICallInfo call)
        {
            // Get the subroutine.
            _subroutine = Controller.GetSubroutine();
            if (_subroutine)
            {
                ParameterHandler = _subroutine.ParameterHandler;
                SetParameters(call);

                // Store the object the subroutine is executing with.
                if (Controller.Attributes.IsInstance)
                {
                    // Normal
                    if (!Controller.Attributes.IsRecursive)
                        ActionSet.AddAction(_subroutine.ObjectStack.SetVariable((Element)ActionSet.CurrentObject));
                    // Recursive: Stack
                    else
                        ActionSet.AddAction(_subroutine.ObjectStack.ModifyVariable(Operation.AppendToArray, Element.CreateArray(ActionSet.CurrentObject)));
                }

                call.ExecuteSubroutine(ActionSet, _subroutine.Subroutine);

                // If a return handler was provided, bridge the return value.
                if (call.ProvidedReturnHandler != null && _subroutine.ReturnHandler != null)
                    call.ProvidedReturnHandler.ReturnValue(_subroutine.ReturnHandler.GetReturnedValue());

                return _subroutine.ReturnHandler?.GetReturnedValue();
            }
            else
            {
                // Inline
                // Recursive stack.
                if (Controller.Attributes.IsRecursive)
                {
                    var lastCall = GetExistingStack();

                    // Function is not yet on the stack.
                    if (lastCall == null)
                        return BuildInline(call);
                    else // Recursive call.
                    {
                        lastCall.RecursiveCall(call, ActionSet);
                        return ActionSet.ReturnHandler.GetReturnedValue();
                    }
                }
                else
                    return BuildInline(call);
            }
        }

        IWorkshopTree BuildInline(ICallInfo call)
        {
            // Create parameter handlers
            ParameterHandler = Controller.CreateParameterHandler(ActionSet, call.Parameters);

            // Setup inline-recursive handler.
            RecursiveStack stack = null;
            if (!_subroutine && Controller.Attributes.IsRecursive)
            {
                stack = new RecursiveStack(this, Controller.StackIdentifier());
                stack.Init();
                ActionSet.Translate.MethodStack.Add(stack);
            }

            ModifySet(a => a.PackThis());

            // Setup the return handler.
            if (call.ProvidedReturnHandler != null)
            {
                ReturnHandler = call.ProvidedReturnHandler;
                ModifySet(a => a.New(call.ProvidedReturnHandler));
            }
            else
                SetupReturnHandler();

            SetParameters(call); // Set the parameters.
            AddParametersToAssigner();
            stack?.StartRecursiveLoop(); // Start the recursion loop.
            Controller.Build(ActionSet); // Build the function contents.
            ReturnHandler.ApplyReturnSkips(); // Returns will skip to this point, right before the recursive loop ends.
            stack?.EndRecursiveLoop(); // End the recursive loop.

            if (stack != null) ActionSet.Translate.MethodStack.Remove(stack); // Remove recursion info from the stack.

            return ReturnHandler.GetReturnedValue();
        }

        public void SetupReturnHandler()
        {
            ReturnHandler = Controller.GetReturnHandler(ActionSet);
            if (ReturnHandler != null)
                ModifySet(a => a.New(ReturnHandler));
        }

        public void AddParametersToAssigner() => ParameterHandler.AddParametersToAssigner(ActionSet.IndexAssigner);

        void SetParameters(ICallInfo call) => ParameterHandler.Set(ActionSet, call.Parameters.Select(p => p.Value).ToArray());
        public void ModifySet(Func<ActionSet, ActionSet> modify) => ActionSet = modify(ActionSet);
        RecursiveStack GetExistingStack() => ActionSet.Translate.MethodStack.FirstOrDefault(stack => stack.Identifier == Controller.StackIdentifier());
    }
}