using System.Linq;
using Deltin.Deltinteger.Parse.Workshop;
using Deltin.Deltinteger.Parse.Functions.Builder;
using Deltin.Deltinteger.Parse.Functions.Builder.User;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse.Types.Constructors
{
    public class DefinedConstructorInstance : Constructor
    {
        public DefinedConstructorProvider Provider { get; }
        public IVariableInstance[] ParameterVars { get; }
        private InstanceAnonymousTypeLinker _typeLinker;

        public DefinedConstructorInstance(
            CodeType typeInstance,
            DefinedConstructorProvider provider,
            InstanceAnonymousTypeLinker typeLinker,
            Location definedAt
        ) : base(typeInstance, definedAt, AccessLevel.Public)
        {
            Provider = provider;
            _typeLinker = typeLinker;

            Parameters = new CodeParameter[provider.ParameterProviders.Length];
            ParameterVars = new IVariableInstance[Parameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
            {
                var parameterInstance = provider.ParameterProviders[i].GetInstance(typeLinker);
                ParameterVars[i] = parameterInstance.Variable;
                Parameters[i] = parameterInstance.Parameter;
            }

            CallInfo = Provider.CallInfo;
            RestrictedValuesAreFatal = Provider.SubroutineName == null;
        }

        public override void Parse(ActionSet actionSet, WorkshopParameter[] parameters)
        {
            WorkshopFunctionBuilder.Call(
                actionSet,
                new MethodCall(parameters),
                new UserConstructorController(this, actionSet.ToWorkshop));
        }

        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            parseInfo.Script.Elements.AddDeclarationCall(Provider, new DeclarationCall(callRange, false));
            parseInfo.CurrentCallInfo.Call(Provider.CallInfo.Function, callRange);
        }

        class UserConstructorController : IWorkshopFunctionController
        {
            public WorkshopFunctionControllerAttributes Attributes { get; } = new WorkshopFunctionControllerAttributes();
            readonly DefinedConstructorInstance _instance;
            readonly ToWorkshop _toWorkshop;

            public UserConstructorController(DefinedConstructorInstance instance, ToWorkshop toWorkshop)
            {
                _instance = instance;
                _toWorkshop = toWorkshop;
            }

            // Build the constructor's block.
            public void Build(ActionSet actionSet) => actionSet.SetThisTypeLinker(_instance._typeLinker).CompileStatement(_instance.Provider.Block);

            // Create the parameter handler for the constructor.
            public IParameterHandler CreateParameterHandler(ActionSet actionSet, WorkshopParameter[] providedParameters)
                => new UserFunctionParameterHandler(actionSet, _instance.Parameters, _instance.ParameterVars, providedParameters, new[] { _instance.ParameterVars });

            // Create the return handler for the constructor.
            public ReturnHandler GetReturnHandler(ActionSet actionSet) => new ReturnHandler(actionSet);

            // Gets the subroutine, or creates it if it does not exist yet.
            public SubroutineCatalogItem GetSubroutine()
            {
                // Do not create a subroutine if the constructor is not a subroutine.
                if (_instance.Provider.SubroutineName == null)
                    return null;

                // The default key used for identifying the subroutine that is linked to this constructor.
                // A similiar implementation of this can be found in the UserFunctionController.
                // Since we are working with just class type-args without the possibility of function type-args,
                // we can just use the provider for a key if there aren't any type-args, else use the class's linked combo.
                object key = _instance.Provider;

                if (_instance._typeLinker != null)
                {
                    // Type linker converted to an array to be used to identify a compatible combo.
                    var rawTypeArgs = _instance._typeLinker.TypeArgsFromAnonymousTypes(_instance.Provider.TypeProvider.GenericTypes);

                    // Get a compatible combo from the type-args from the class.
                    var combo = _toWorkshop.TypeArgGlob.Trackers[_instance.Provider.TypeProvider].GetCompatibleCombo(rawTypeArgs);

                    // Use the combo as a key if it is not null.
                    if (combo != null)
                        key = combo;
                }

                return _toWorkshop.SubroutineCatalog.GetSubroutine(key, () =>
                    new(new SubroutineBuilder(_toWorkshop.DeltinScript, new()
                    {
                        ContainingType = _instance.Type,
                        Controller = this,
                        RuleName = _instance.Provider.SubroutineName,
                        ElementName = _instance.Type.GetName() + "_constructor",
                        VariableGlobalDefault = true,
                        TypeLinker = _instance._typeLinker
                    })));
            }

            // Unique stack identifier for recursive constructors.
            // This doesn't matter right now since recursive constructors are not supported.
            public object StackIdentifier() => _instance.Provider;
        }
    }
}