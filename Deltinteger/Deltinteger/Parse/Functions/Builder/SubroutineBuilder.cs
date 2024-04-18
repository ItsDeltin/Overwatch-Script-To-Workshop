using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    public class SubroutineBuilder
    {
        public SubroutineCatalogItem Result { get; private set; }

        readonly DeltinScript _deltinScript;
        readonly SubroutineContext _context;

        TranslateRule _subroutineRule;
        ActionSet _actionSet;
        IParameterHandler _parameterHandler;
        WorkshopFunctionBuilder _functionBuilder;
        IGettable _objectStore;

        public SubroutineBuilder(DeltinScript deltinScript, SubroutineContext context)
        {
            _deltinScript = deltinScript;
            _context = context;
        }

        public SubroutineCatalogItem Initiate()
        {
            // Setup the subroutine element.
            Subroutine subroutine = _context.TargetSubroutine;
            subroutine ??= _deltinScript.SubroutineCollection.NewSubroutine(_context.ElementName);

            // Create the rule.
            _subroutineRule = new TranslateRule(_deltinScript, subroutine, _context.RuleName, _context.VariableGlobalDefault);

            // Setup the return handler.
            _actionSet = _subroutineRule.ActionSet
                .ContainVariableAssigner()
                .SetThisTypeLinker(_context.TypeLinker)
                .New(_context.Controller.Attributes.IsRecursive);

            // Create the function builder.
            var controller = _context.Controller;

            // Create the parameter handlers.
            _parameterHandler = controller.CreateParameterHandler(_actionSet, null);

            // If the subroutine is an object function inside a class, create a variable to store the class object.
            if (controller.Attributes.IsInstance)
            {
                _objectStore = _context.GenerateObjectStackStrategy switch
                {
                    // This is required if the object store is meant to be a struct.
                    GenerateObjectStackStrategy.UseContainingType => _context.ContainingType.GetGettableAssigner(new()
                    {
                        Name = _context.ObjectStackName,
                        Extended = !controller.Attributes.IsRecursive && _deltinScript.Settings.SubroutineStacksAreExtended,
                        IsGlobal = _context.VariableGlobalDefault,
                        StoreType = StoreType.FullVariable,
                        VariableType = VariableType.Dynamic
                    }).GetValue(new GettableAssignerValueInfo(_actionSet)
                    {
                        SetInitialValue = SetInitialValue.DoNotSet,
                        IsRecursive = false
                    }),
                    // Simply one register. Used by lambdas which are instances but have no containing type.
                    GenerateObjectStackStrategy.OneRegister or _ => _actionSet.VarCollection.Assign(
                        name: _context.ObjectStackName,
                        isGlobal: _context.VariableGlobalDefault,
                        extended: !controller.Attributes.IsRecursive && _deltinScript.Settings.SubroutineStacksAreExtended),
                };

                // Set the objectStore as an empty array if the subroutine is recursive.
                if (controller.Attributes.IsRecursive)
                {
                    // Initialize as empty array.
                    _objectStore.Set(_actionSet.InitialSet(), Element.EmptyArray());

                    // Add to assigner with the last of the objectStore stack being the object instance.
                    _context.ContainingType?.AddObjectVariablesToAssigner(_actionSet.ToWorkshop, new(Element.LastOf(_objectStore.GetVariable())), _actionSet.IndexAssigner);

                    // Set the actionSet.
                    var selfReference = StructHelper.BridgeIfRequired(_objectStore.GetVariable(), Element.LastOf);
                    var selfSource = new SourceIndexReference(_objectStore.ChildFromClassReference(Element.CountOf(StructHelper.ExtractArbritraryValue(_objectStore.GetVariable())) - 1));
                    _actionSet = _actionSet.New(selfReference).PackThis().New(selfSource);
                }
                else
                {
                    // Add to assigner with the objectStore being the object instance.
                    _context.ContainingType?.AddObjectVariablesToAssigner(_actionSet.ToWorkshop, new(_objectStore), _actionSet.IndexAssigner);

                    // Set the actionSet.
                    _actionSet = _actionSet.New(_objectStore.GetVariable()).PackThis().New(new SourceIndexReference(_objectStore));
                }
            }

            _functionBuilder = new WorkshopFunctionBuilder(_actionSet, controller);
            _functionBuilder.ModifySet(a => a.PackThis()); // TODO: is this required?
            _functionBuilder.SetupReturnHandler();
            _parameterHandler.AddParametersToAssigner(_actionSet.IndexAssigner);

            // Done.
            return Result = new SubroutineCatalogItem(
                subroutine: subroutine,
                parameterHandler: _parameterHandler,
                objectStack: _objectStore,
                returnHandler: _functionBuilder.ReturnHandler);
        }

        public void Complete()
        {
            _functionBuilder.Controller.Build(_functionBuilder.ActionSet);
            _functionBuilder.ReturnHandler?.ApplyReturnSkips();

            if (_context.Controller.Attributes.IsRecursive)
            {
                _parameterHandler.Pop(_actionSet);

                // Pop object array.
                if (_context.Controller.Attributes.IsInstance)
                    _objectStore.Modify(_actionSet, Operation.RemoveFromArrayByIndex, Element.CountOf(_objectStore.GetVariable()) - 1, Element.EventPlayer());
            }

            // Add the subroutine.
            Rule translatedRule = _subroutineRule.GetRule();
            _deltinScript.WorkshopRules.Add(_deltinScript.GetRule(translatedRule));
        }
    }

    public struct SubroutineContext
    {
        public string RuleName;
        public string ElementName;
        public string ObjectStackName;
        public bool VariableGlobalDefault;
        public CodeType ContainingType;
        public IWorkshopFunctionController Controller;
        public InstanceAnonymousTypeLinker TypeLinker;
        public Subroutine TargetSubroutine;
        public GenerateObjectStackStrategy GenerateObjectStackStrategy;
    }

    public enum GenerateObjectStackStrategy
    {
        UseContainingType,
        OneRegister
    }
}