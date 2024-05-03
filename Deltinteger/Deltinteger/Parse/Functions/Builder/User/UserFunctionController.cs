using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Functions.Builder.Virtual;
using Deltin.Deltinteger.Parse.Workshop;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder.User
{
    // Converts a user-defined function to the workshop.
    public class UserFunctionController : IWorkshopFunctionController
    {
        // The attributes of the function.
        public WorkshopFunctionControllerAttributes Attributes { get; } = new WorkshopFunctionControllerAttributes();

        readonly ToWorkshop _toWorkshop;
        readonly DefinedMethodInstance _function;
        readonly InstanceAnonymousTypeLinker _typeArgLinker;
        readonly InstanceAnonymousTypeLinker _calleeThisTypeLinker;
        readonly ClassWorkshopRelation _classRelation;
        readonly List<DefinedMethodInstance> _allVirtualOptions = new List<DefinedMethodInstance>();

        public UserFunctionController(ToWorkshop toWorkshop, DefinedMethodInstance function, InstanceAnonymousTypeLinker typeArgs, InstanceAnonymousTypeLinker calleeThisTypeLinker)
        {
            _toWorkshop = toWorkshop;
            _function = function;
            _typeArgLinker = typeArgs;
            _calleeThisTypeLinker = calleeThisTypeLinker;
            _allVirtualOptions.Add(function);

            Attributes.IsRecursive = function.Attributes.Recursive;

            // If the function is defined in a type.
            if (function.HasContainingType())
            {
                Attributes.IsInstance = true;

                var relations = new MethodClassRelations(toWorkshop, function, calleeThisTypeLinker);

                // Get the class relation.
                _classRelation = relations.ClassRelation;

                // Extract the virtual functions.
                _allVirtualOptions.AddRange(relations.Overriders);
            }
        }

        // Creates a return handler.
        public ReturnHandler GetReturnHandler(ActionSet actionSet) => new ReturnHandler(
            actionSet,
            _function.CodeType?.GetCodeType(actionSet.DeltinScript)
                               .GetRealType(_typeArgLinker)
                               .GetGettableAssigner(new AssigningAttributes("returnValue_" + _function.Name, actionSet.IsGlobal, false))
                               .GetValue(new GettableAssignerValueInfo(actionSet)
                               {
                                   SetInitialValue = SetInitialValue.DoNotSet,
                                   Inline = !IsMultiplePaths()
                               }),
            IsMultiplePaths());

        bool IsMultiplePaths() => _function.Provider.ReturnType != null &&
            (_function.Provider.MultiplePaths || _function.Attributes.Recursive || _function.Provider.SubroutineName != null || _function.Provider.Virtual);

        // Creates parameters assigned to this function.
        public IParameterHandler CreateParameterHandler(ActionSet actionSet, WorkshopParameter[] providedParameters)
            => new UserFunctionParameterHandler(
                actionSet,
                _function.Parameters,
                _function.ParameterVars,
                providedParameters,
                from virtualOption in _allVirtualOptions select virtualOption.ParameterVars
            );

        // Create or get the subroutine.
        public SubroutineCatalogItem GetSubroutine()
        {
            // Not a subroutine.
            if (!_function.Provider.IsSubroutine)
                return null;

            var providedTypeArgs = _typeArgLinker?.TypeArgsFromAnonymousTypes(_function.Provider.GenericTypes);

            // The subroutine identifier is used to determine if a compatible subroutine was already created.
            var identifier = new UniqueSubroutineIdentifier(
                // The function's provider.
                _function.Provider,
                // The combo of the contained class.
                _classRelation?.Combo,
                // The combo of the type args.
                _typeArgLinker == null ? null : _toWorkshop.TypeArgGlob.ComboFromKeyAndArgs(_function.Provider, providedTypeArgs)
            );

            Subroutine targetSubroutine = null;
            if (_function.Provider.TargetVanillaSubroutine is not null)
                targetSubroutine = _toWorkshop.LinkableVanillaVariables.GetSubroutine(_function.Provider.TargetVanillaSubroutine.Value.Name);

            // Get or create the subroutine.
            return _toWorkshop.SubroutineCatalog.GetSubroutine(identifier, () =>
                // Create the subroutine.
                new(new SubroutineBuilder(_toWorkshop.DeltinScript, new()
                {
                    Controller = this,
                    ElementName = _function.Name,
                    RuleName = _function.Provider.SubroutineName,
                    ObjectStackName = _function.Name + "Stack",
                    VariableGlobalDefault = _function.Provider.SubroutineDefaultGlobal,
                    ContainingType = _function.GetContainingType(_calleeThisTypeLinker),
                    TypeLinker = _typeArgLinker,
                    TargetSubroutine = targetSubroutine
                }))
            );
        }

        public object StackIdentifier() => _function;

        public void Build(ActionSet actionSet) => new MethodContentBuilder(
            actionSet: actionSet,
            functions: from virtualOption in _allVirtualOptions select new UserFunctionBuilder(virtualOption, _calleeThisTypeLinker)
        );

        class UserFunctionBuilder : IVirtualMethodHandler
        {
            readonly DefinedMethodInstance _method;
            readonly ClassType _containingType;

            public UserFunctionBuilder(DefinedMethodInstance method, InstanceAnonymousTypeLinker calleeThisTypeLinker)
            {
                _method = method;
                _containingType = method.GetContainingType(calleeThisTypeLinker) as ClassType;
            }

            public void Build(ActionSet actionSet) => actionSet.CompileStatement(_method.Provider.Block);
            public ClassType ContainingType() => _containingType;
        }

        // This class is used as the key for identifying existing compatible subroutines.
        class UniqueSubroutineIdentifier
        {
            readonly DefinedMethodProvider _provider;
            readonly WorkshopInitializedCombo _classCombo;
            readonly TypeArgCombo _functionCombo;

            public UniqueSubroutineIdentifier(DefinedMethodProvider provider, WorkshopInitializedCombo classCombo, TypeArgCombo functionCombo)
            {
                _provider = provider;
                _classCombo = classCombo;
                _functionCombo = functionCombo;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;

                var other = (UniqueSubroutineIdentifier)obj;
                return _provider == other._provider && _classCombo == other._classCombo && _functionCombo == other._functionCombo;
            }

            // Create the hash code from the object references of the input fields.
            public override int GetHashCode() => HashCode.Combine(_provider, _classCombo, _functionCombo);
        }
    }

    class UserFunctionParameterHandler : IParameterHandler
    {
        readonly UserFunctionParameter[] _parameters;
        readonly CodeParameter[] _codeParameters;
        readonly bool _recursiveParameters;

        public UserFunctionParameterHandler(
            ActionSet actionSet,
            CodeParameter[] codeParameters,
            IVariableInstance[] parameterVariables,
            WorkshopParameter[] providedParameters,
            IEnumerable<IVariableInstance[]> virtualParameterVariables)
        {
            _recursiveParameters = actionSet.IsRecursive;
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
                        .GetAssigner(new(actionSet))
                        .GetValue(new GettableAssignerValueInfo(actionSet)
                        {
                            ForceNonpersistentClear = _recursiveParameters,
                            SetInitialValue = SetInitialValue.DoNotSet,
                            InitialValueOverride = providedParameters?[i].Value
                        });

                _parameters[i] = new UserFunctionParameter(
                    gettable,
                    virtualParameterVariables.Select(v => v[i].Provider).ToArray());
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

            public void Set(ActionSet actionSet, IWorkshopTree value)
            {
                if (Gettable.CanBeSet())
                    Gettable.Set(actionSet, value);
            }
            public void Push(ActionSet actionSet, IWorkshopTree value) => Gettable.Push(actionSet, value);
            public void Pop(ActionSet actionSet) => Gettable.Pop(actionSet);
        }
    }
}