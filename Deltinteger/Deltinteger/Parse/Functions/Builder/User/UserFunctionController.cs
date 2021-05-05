using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Workshop;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder.User
{
    // Converts a user-defined function to the workshop.
    public class UserFunctionController : IWorkshopFunctionController
    {
        // The attributes of the function.
        public WorkshopFunctionControllerAttributes Attributes { get; } = new WorkshopFunctionControllerAttributes();

        readonly DefinedMethodInstance _function;
        readonly DeltinScript _deltinScript;
        readonly ClassWorkshopRelation _classRelation;
        readonly List<DefinedMethodInstance> _allVirtualOptions = new List<DefinedMethodInstance>();

        public UserFunctionController(DeltinScript deltinScript, ToWorkshop toWorkshop, DefinedMethodInstance function)
        {
            _function = function;
            _deltinScript = deltinScript;
            _allVirtualOptions.Add(function);

            // If the function is defined in a type.
            if (function.DefinedInType != null)
            {
                Attributes.IsInstance = true;

                // Get the class relation.
                _classRelation = toWorkshop.ClassInitializer.RelationFromClassType((ClassType)function.DefinedInType);

                // Get the virtual functions.
                foreach (var extender in _classRelation.GetAllExtenders())
                {
                    // Extract the virtual function.
                    var methodInstance = extender.Instance.Elements.ScopeableElements.FirstOrDefault(element =>
                        // Make sure the scopeable is a defined method...
                        element.Scopeable is DefinedMethodInstance method &&
                        // ...that overrides the target method.
                        DoesOverride(function, method)
                    ).Scopeable as DefinedMethodInstance;

                    // Add the method instance if it exists.
                    // todo: In _allVirtualOptions, it may be a good idea to include classes that do not override so we don't need to check auto-implementations in the virtual builder.
                    if (methodInstance != null)
                    {
                        _allVirtualOptions.Add(methodInstance);
                    }
                }
            }
        }

        static bool DoesOverride(DefinedMethodInstance target, DefinedMethodInstance overrider)
        {
            while (overrider != null)
            {
                if (overrider.Provider == target.Provider) return true;
                overrider = overrider.Provider.OverridingFunction;
            }
            return false;
        }

        // Creates a return handler.
        public ReturnHandler GetReturnHandler(ActionSet actionSet) => new ReturnHandler(
            actionSet,
            _function.CodeType?.GetCodeType(actionSet.DeltinScript)
                               .GetGettableAssigner(new AssigningAttributes("returnValue_" + _function.Name, actionSet.IsGlobal, false))
                               .GetValue(new GettableAssignerValueInfo(actionSet) { SetInitialValue = false }),
            IsMultiplePaths());
        // Todo: virtual and subroutine checks.
        bool IsMultiplePaths() => _function.Provider.ReturnType != null && (_function.Provider.MultiplePaths || _function.Attributes.Recursive);

        // Creates parameters assigned to this function.
        public IParameterHandler CreateParameterHandler(ActionSet actionSet, WorkshopParameter[] providedParameters)
            => new UserFunctionParameterHandler(actionSet, _function.Parameters, _function.ParameterVars, providedParameters);

        // Create or get the subroutine.
        public SubroutineCatalogItem GetSubroutine()
        {
            // Not a subroutine.
            if (!_function.Provider.IsSubroutine)
                return null;
            
            // Get or create the subroutine.
            return _deltinScript.GetComponent<SubroutineCatalog>().GetSubroutine(_classRelation.Combo, () =>
                // Create the subroutine.
                new SubroutineBuilder(_deltinScript, new() {
                    Controller = this,
                    ElementName = _function.Name,
                    RuleName = _function.Provider.SubroutineName,
                    ObjectStackName = _function.Name + "Stack",
                    VariableGlobalDefault = _function.Provider.SubroutineDefaultGlobal,
                    // TODO: use _function.ContainingType (when that is ready)
                    ContainingType = _function.DefinedInType
                    // ContainingType = _function.Provider.ContainingType?.GetInstance(_function.InstanceInfo)
                }).SetupSubroutine()
            );
        }

        public object StackIdentifier() => _function;

        public void Build(ActionSet actionSet)
        {
            // Create the function builder.
            var virtualContentBuilder = new VirtualContentBuilder(
                actionSet: actionSet,
                functions: from virtualOption in _allVirtualOptions select new UserFunctionBuilder(virtualOption)
            );
            virtualContentBuilder.Build();
        }

        class UserFunctionBuilder : IVirtualFunctionHandler
        {
            readonly DefinedMethodInstance _method;

            public UserFunctionBuilder(DefinedMethodInstance method)
            {
                _method = method;
            }

            public void Build(ActionSet actionSet) => _method.Provider.Block.Translate(actionSet.SetThisTypeLinker(_method.InstanceInfo));
            public ClassType ContainingType() => (ClassType)_method.DefinedInType;
        }
    }

    class UserFunctionParameterHandler : IParameterHandler
    {
        readonly UserFunctionParameter[] _parameters;
        readonly CodeParameter[] _codeParameters;

        public UserFunctionParameterHandler(
            ActionSet actionSet,
            CodeParameter[] codeParameters,
            IVariableInstance[] parameterVariables,
            WorkshopParameter[] providedParameters)
        {
            _codeParameters = codeParameters;
            _parameters = new UserFunctionParameter[codeParameters.Length];
            for (int i = 0; i < _parameters.Length; i++)
            {
                // Get the gettable provided from the ref parameter.
                IGettable gettable = providedParameters?[i].RefVariableElements?.Childify();

                // Not provided or not a ref parameter.
                if (gettable == null)
                    // Create a gettable for the parameter.
                    gettable = parameterVariables[i]
                        .GetAssigner(actionSet)
                        .GetValue(new GettableAssignerValueInfo(actionSet) { SetInitialValue = false });

                _parameters[i] = new UserFunctionParameter(gettable, new[] { parameterVariables[i].Provider }); //todo: linkedVariables for virtual
            }
        }

        public void Set(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            if (parameterValues.Length != _parameters.Length)
                throw new Exception("Parameter count mismatch");
            
            for (int i = 0; i < _parameters.Length; i++)
                if (!_codeParameters[i].Attributes.Ref)
                    _parameters[i].Set(actionSet, parameterValues[i]);
        }

        public void Push(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            if (parameterValues.Length != _parameters.Length)
                throw new Exception("Parameter count mismatch");
            
            for (int i = 0; i < _parameters.Length; i++)
                _parameters[i].Push(actionSet, parameterValues[i]);
        }

        public void Pop(ActionSet actionSet)
        {
            foreach (var parameter in _parameters)
                parameter.Pop(actionSet);
        }

        public void AddParametersToAssigner(VarIndexAssigner assigner)
        {
            foreach (var parameter in _parameters)
                foreach (var linked in parameter.LinkedVariables)
                    assigner.Add(linked, parameter.Gettable);
        }

        struct UserFunctionParameter
        {
            public readonly IGettable Gettable;
            public readonly IVariable[] LinkedVariables;

            public UserFunctionParameter(IGettable gettable, IVariable[] linkedVariables)
            {
                Gettable = gettable;
                LinkedVariables = linkedVariables;
            }

            public void Set(ActionSet actionSet, IWorkshopTree value) => Gettable.Set(actionSet, value);
            public void Push(ActionSet actionSet, IWorkshopTree value)
            {
                if (Gettable is RecursiveIndexReference recursive)
                    actionSet.AddAction(recursive.Push((Element)value));
                else
                    throw new Exception("Cannot push non-recursive parameter");
            }
            public void Pop(ActionSet actionSet)
            {
                if (Gettable is RecursiveIndexReference recursive)
                    actionSet.AddAction(recursive.Pop());
                else
                    throw new Exception("Cannot pop non-recursive parameter");
            }
        }
    }
}