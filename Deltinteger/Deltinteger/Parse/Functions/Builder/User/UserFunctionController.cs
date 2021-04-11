using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Workshop;

namespace Deltin.Deltinteger.Parse.Functions.Builder.User
{
    // Converts a user-defined function to the workshop.
    public class UserFunctionController : IWorkshopFunctionController
    {
        // The attributes of the function.
        public WorkshopFunctionControllerAttributes Attributes { get; } = new WorkshopFunctionControllerAttributes();

        readonly DefinedMethodInstance _function;
        readonly DeltinScript _deltinScript;
        readonly List<DefinedMethodInstance> _overriders = new List<DefinedMethodInstance>();

        public UserFunctionController(DeltinScript deltinScript, ToWorkshop toWorkshop, DefinedMethodInstance function)
        {
            _function = function;
            _deltinScript = deltinScript;
        }

        void GetOverridersOf(CompileRelations relations, DefinedMethodInstance method)
        {
            // Get the overrider's instances.
            var overriders =
                // Get the overriders and convert them to DefinedMethodProviders.
                Array.ConvertAll(relations.GetOverridersOf(method.Provider), p => (DefinedMethodProvider)p)
                // Create instances from the provider. Use the original method's instance info.
                .Select(provider => provider.CreateInstance(method.InstanceInfo));

            foreach (var overrider in overriders)
            {
                _overriders.Add(overrider);

                // Recursively get the overrider's overriders.
                GetOverridersOf(relations, overrider);
            }
        }

        // Creates a return handler.
        public ReturnHandler GetReturnHandler(ActionSet actionSet) => new ReturnHandler(actionSet, _function.Name, _function.CodeType.GetCodeType(actionSet.DeltinScript), IsMultiplePaths());
        // Todo: virtual and subroutine checks.
        bool IsMultiplePaths() => _function.Provider.ReturnType != null && (_function.Provider.MultiplePaths || _function.Attributes.Recursive);

        // Creates parameters assigned to this function.
        public IParameterHandler[] CreateParameterHandlers(ActionSet actionSet)
        {
            var parameters = new UserParameterHandler[_function.Parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                // Create a gettable for the parameter.
                var gettable = _function.Parameters[i]
                    .GetCodeType(_deltinScript)
                    .GetGettableAssigner(_function.ParameterVars[i].Provider)
                    .GetValue(new GettableAssignerValueInfo(actionSet));

                parameters[i] = new UserParameterHandler(gettable, null); //todo: linkedVariables
            }

            return parameters;
        }

        // Create or get the subroutine.
        public SubroutineCatalogItem GetSubroutine()
        {
            // Not a subroutine.
            if (!_function.Provider.IsSubroutine)
                return null;
            
            // Get or create the subroutine.
            return _deltinScript.GetComponent<SubroutineCatalog>().GetSubroutine(_function.Provider, () => {
                // Create the subroutine.
            });
        }

        public object StackIdentifier() => _function;

        public void Build()
        {
            throw new System.NotImplementedException();
        }

        class UserParameterHandler : ParameterHandler
        {
            readonly IVariable[] _linkedVariables;

            public UserParameterHandler(IGettable gettable, IVariable[] linkedVariables) : base(gettable)
            {
                _linkedVariables = linkedVariables;
            }

            public override void AddToAssigner(VarIndexAssigner assigner)
            {
                foreach (var linked in _linkedVariables)
                    assigner.Add(linked, _gettable);
            }
        }
    }
}