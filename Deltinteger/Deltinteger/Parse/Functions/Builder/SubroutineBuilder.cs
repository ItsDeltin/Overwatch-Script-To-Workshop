using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    class SubroutineBuilder
    {
        public SubroutineCatalogItem Result { get; private set; }

        readonly DeltinScript _deltinScript;
        readonly SubroutineContext _context;

        public SubroutineBuilder(DeltinScript deltinScript, SubroutineContext context)
        {
            _deltinScript = deltinScript;
            _context = context;
        }

        public SubroutineCatalogItem SetupSubroutine()
        {
            // Setup the subroutine element.
            Subroutine subroutine = _deltinScript.SubroutineCollection.NewSubroutine(_context.ElementName);

            // Create the rule.
            TranslateRule subroutineRule = new TranslateRule(_deltinScript, subroutine, _context.RuleName, _context.VariableGlobalDefault);

            // Setup the return handler.
            ActionSet actionSet = subroutineRule.ActionSet.New(subroutineRule.ActionSet.IndexAssigner.CreateContained()).SetThisTypeLinker(_context.TypeLinker);

            // Create the function builder.
            var controller = _context.Controller;

            // Create the parameter handlers.
            var parameterHandler = controller.CreateParameterHandler(actionSet, null);
            
            // If the subroutine is an object function inside a class, create a variable to store the class object.
            IndexReference objectStore = null;
            if (controller.Attributes.IsInstance)
            {
                objectStore = actionSet.VarCollection.Assign(_context.ObjectStackName, true, !controller.Attributes.IsRecursive);

                // Set the objectStore as an empty array if the subroutine is recursive.
                if (controller.Attributes.IsRecursive)
                {
                    // Initialize as empty array.
                    actionSet.InitialSet().AddAction(objectStore.SetVariable(Element.EmptyArray()));

                    // Add to assigner with the last of the objectStore stack being the object instance.
                    _context.ContainingType?.AddObjectVariablesToAssigner(actionSet.ToWorkshop, Element.LastOf(objectStore.GetVariable()), actionSet.IndexAssigner);

                    // Set the actionSet.
                    actionSet = actionSet.New(Element.LastOf(objectStore.Get())).PackThis().New(objectStore.CreateChild(Element.CountOf(objectStore.Get()) - 1));
                }
                else
                {
                    // Add to assigner with the objectStore being the object instance.
                    _context.ContainingType?.AddObjectVariablesToAssigner(actionSet.ToWorkshop, objectStore.GetVariable(), actionSet.IndexAssigner);

                    // Set the actionSet.
                    actionSet = actionSet.New(objectStore.Get()).PackThis().New(objectStore);
                }
            }

            var functionBuilder = new WorkshopFunctionBuilder(actionSet, controller);
            functionBuilder.ModifySet(a => a.PackThis()); // TODO: is this required?
            functionBuilder.SetupReturnHandler();
            parameterHandler.AddParametersToAssigner(actionSet.IndexAssigner);
            functionBuilder.Controller.Build(functionBuilder.ActionSet); 
            functionBuilder.ReturnHandler?.ApplyReturnSkips();

            // Pop object array if recursive.
            if (controller.Attributes.IsRecursive && controller.Attributes.IsInstance)
                actionSet.AddAction(objectStore.ModifyVariable(Operation.RemoveFromArrayByIndex, Element.CountOf(objectStore.GetVariable()) - 1));

            // Add the subroutine.
            Rule translatedRule = subroutineRule.GetRule();
            _deltinScript.WorkshopRules.Add(translatedRule);

            // Done.
            return Result = new SubroutineCatalogItem(
                subroutine: subroutine,
                parameterHandler: parameterHandler,
                objectStack: objectStore,
                returnHandler: functionBuilder.ReturnHandler);
        }
    }

    struct SubroutineContext
    {
        public string RuleName;
        public string ElementName;
        public string ObjectStackName;
        public bool VariableGlobalDefault;
        public CodeType ContainingType;
        public IWorkshopFunctionController Controller;
        public InstanceAnonymousTypeLinker TypeLinker;
    }
}