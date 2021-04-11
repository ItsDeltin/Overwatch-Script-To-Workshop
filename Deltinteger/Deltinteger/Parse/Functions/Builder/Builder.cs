using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    public class WorkshopFunctionBuilder
    {
        // Creates and calls a WorkshopFunctionBuilder.
        public static IWorkshopTree Call(ActionSet actionSet, ICallInfo call, IWorkshopFunctionController controller)
        {
            var builder = new WorkshopFunctionBuilder(actionSet, call, controller);
            return builder.Call();
        }

        public ActionSet ActionSet { get; private set; }
        public ICallInfo CallInfo { get; }
        public IWorkshopFunctionController Controller { get; }
        public IParameterHandler[] ParameterHandlers { get; private set; }
        SubroutineCatalogItem _subroutine;
        ReturnHandler _returnHandler;

        private WorkshopFunctionBuilder(ActionSet actionSet, ICallInfo call, IWorkshopFunctionController controller)
        {
            ActionSet = actionSet;
            CallInfo = call;
            Controller = controller;
        }

        public IWorkshopTree Call()
        {
            // Get the subroutine.
            _subroutine = Controller.GetSubroutine();
            if (_subroutine)
            {
                ParameterHandlers = _subroutine.Parameters;
                SetParameters();

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

                ExecuteSubroutine(_subroutine.Subroutine, CallHandler.ParallelMode);
                return _subroutine.ReturnHandler.GetReturnedValue();
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
                        return BuildInline();
                    else // Recursive call.
                    {
                        lastCall.RecursiveCall(CallInfo, ActionSet);
                        return ActionSet.ReturnHandler.GetReturnedValue();
                    }
                }
                else
                    return BuildInline();
            }
        }

        IWorkshopTree BuildInline()
        {
            // Create parameter handlers
            ParameterHandlers = Controller.CreateParameterHandlers(ActionSet);
            return Build();
        }

        public IWorkshopTree Build()
        {
            // Setup inline-recursive handler.
            RecursiveStack stack = null;
            if (!_subroutine && Controller.Attributes.IsRecursive)
            {
                stack = new RecursiveStack(this, Controller.StackIdentifier());
                stack.Init();
                ActionSet.Translate.MethodStack.Add(stack);
            }

            ModifySet(a => a.PackThis());
            SetupReturnHandler(); // Setup the return handler.
            SetParameters(); // Set the parameters.
            stack?.StartRecursiveLoop(); // Start the recursion loop.
            Controller.Build(); // Build the function contents.
            _returnHandler.ApplyReturnSkips(); // Returns will skip to this point, right before the recursive loop ends.
            stack?.EndRecursiveLoop(); // End the recursive loop.

            if (stack != null) ActionSet.Translate.MethodStack.Remove(stack); // Remove recursion info from the stack.

            return _returnHandler.GetReturnedValue();
        }

        void SetupReturnHandler()
        {
            _returnHandler = Controller.GetReturnHandler(ActionSet);
            ModifySet(a => a.New(_returnHandler));
        }

        void SetParameters()
        {
            // Ensure the parameter counts are the same.
            if (ParameterHandlers.Length != CallInfo.Parameters.Length)
                throw new Exception("Parameter count mismatch");

            for (int i = 0; i < ParameterHandlers.Length; i++)
                ParameterHandlers[i].Set(ActionSet, CallInfo.Parameters[i]);
        }

        public void PushParameters(ICallInfo callInfo)
        {
            // Ensure the parameter counts are the same.
            if (ParameterHandlers.Length != CallInfo.Parameters.Length)
                throw new Exception("Parameter count mismatch");

            for (int i = 0; i < ParameterHandlers.Length; i++)
                ParameterHandlers[i].Push(ActionSet, callInfo.Parameters[i]);
        }

        public void PopParameters()
        {
            // Ensure the parameter counts are the same.
            if (ParameterHandlers.Length != CallInfo.Parameters.Length)
                throw new Exception("Parameter count mismatch");

            for (int i = 0; i < ParameterHandlers.Length; i++)
                ParameterHandlers[i].Pop(ActionSet);
        }

        public void ModifySet(Func<ActionSet, ActionSet> modify) => ActionSet = modify(ActionSet);

        RecursiveStack GetExistingStack()
        {
        }
    }
}