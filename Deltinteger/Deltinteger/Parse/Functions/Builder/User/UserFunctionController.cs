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
        readonly DefinedMethodInstance[] _allVirtualOptions;

        public UserFunctionController(DeltinScript deltinScript, ToWorkshop toWorkshop, DefinedMethodInstance function)
        {
            _function = function;
            _deltinScript = deltinScript;
            _allVirtualOptions = toWorkshop.Relations.GetAllOverridersOf(function);
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
        public IParameterHandler CreateParameterHandler(ActionSet actionSet) => new UserFunctionParameterHandler(actionSet, _function.Parameters, _function.ParameterVars);

        // Create or get the subroutine.
        public SubroutineCatalogItem GetSubroutine()
        {
            // Not a subroutine.
            if (!_function.Provider.IsSubroutine)
                return null;
            
            // Get or create the subroutine.
            return _deltinScript.GetComponent<SubroutineCatalog>().GetSubroutine(_function.Provider, () =>
                // Create the subroutine.
                new SubroutineBuilder(_deltinScript, new() {
                    Controller = this,
                    ElementName = _function.Name,
                    RuleName = _function.Provider.SubroutineName,
                    ObjectStackName = _function.Name + "Stack",
                    VariableGlobalDefault = _function.Provider.SubroutineDefaultGlobal,
                    // TODO: use _function.ContainingType (when that is ready)
                    ContainingType = _function.Provider.ContainingType?.GetInstance(_function.InstanceInfo)
                }).SetupSubroutine()
            );
        }

        public object StackIdentifier() => _function;

        public void Build(ActionSet actionSet)
        {
            // Create the function builder.
            var virtualContentBuilder = new VirtualContentBuilder(
                actionSet: actionSet,
                functions: from virtualOption in _allVirtualOptions select new UserFunctionBuilder(virtualOption, _deltinScript)
            );
            virtualContentBuilder.Build();
        }

        class UserFunctionBuilder : IVirtualFunctionHandler
        {
            readonly DefinedMethodInstance _method;
            readonly ClassType _type;

            public UserFunctionBuilder(DefinedMethodInstance method, DeltinScript deltinScript)
            {
                _method = method;

                // Function is inside a class.
                if (_method.Provider.ContainingType != null)
                    _type = (ClassType)_method.Provider.ContainingType.GetInstance(_method.InstanceInfo);
            }

            public void Build(ActionSet actionSet) => _method.Provider.Block.Translate(actionSet);
            public ClassType ContainingType() => _type;
        }
    }

    class UserFunctionParameterHandler : IParameterHandler
    {
        readonly UserFunctionParameter[] _parameters;

        public UserFunctionParameterHandler(ActionSet actionSet, CodeParameter[] parameters, IVariableInstance[] parameterVariables)
        {
            _parameters = new UserFunctionParameter[parameters.Length];
            for (int i = 0; i < _parameters.Length; i++)
            {
                // Create a gettable for the parameter.
                var gettable = parameterVariables[i].GetAssigner(actionSet)
                                                    .GetValue(new GettableAssignerValueInfo(actionSet) { SetInitialValue = false });

                _parameters[i] = new UserFunctionParameter(gettable, new[] { parameterVariables[i].Provider }); //todo: linkedVariables for virtual
            }
        }

        public void Set(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            if (parameterValues.Length != _parameters.Length)
                throw new Exception("Parameter count mismatch");
            
            for (int i = 0; i < _parameters.Length; i++)
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