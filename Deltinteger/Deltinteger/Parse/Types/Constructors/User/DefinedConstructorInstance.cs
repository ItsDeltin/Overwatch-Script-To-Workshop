using System.Linq;
using Deltin.Deltinteger.Parse.Functions.Builder;
using Deltin.Deltinteger.Parse.Functions.Builder.User;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse.Types.Constructors
{
    public class DefinedConstructorInstance : Constructor
    {
        public DefinedConstructorProvider Provider { get; }
        public IVariableInstance[] ParameterVars { get; }

        public DefinedConstructorInstance(
            CodeType typeInstance,
            DefinedConstructorProvider provider,
            InstanceAnonymousTypeLinker genericsLinker,
            Location definedAt
        ) : base(typeInstance, definedAt, AccessLevel.Public)
        {
            Provider = provider;
            
            Parameters = new CodeParameter[provider.ParameterProviders.Length];
            ParameterVars = new IVariableInstance[Parameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
            {
                var parameterInstance = provider.ParameterProviders[i].GetInstance(genericsLinker);
                ParameterVars[i] = parameterInstance.Variable;
                Parameters[i] = parameterInstance.Parameter;
            }
        }

        public override void Parse(ActionSet actionSet, WorkshopParameter[] parameters)
        {
            WorkshopFunctionBuilder.Call(
                actionSet,
                new Functions.Builder.CallInfo(parameters.Select(p => p.Value).ToArray()),
                new UserConstructorController(this, actionSet.DeltinScript));
        }

        /*
        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            parseInfo.CurrentCallInfo?.Call(_recursiveCallHandler, callRange);
            Type.AddLink(parseInfo.GetLocation(callRange));
        }
        */

        class UserConstructorController : IWorkshopFunctionController
        {
            public WorkshopFunctionControllerAttributes Attributes { get; }
            readonly DefinedConstructorInstance _instance;
            readonly DeltinScript _deltinScript;

            public UserConstructorController(DefinedConstructorInstance instance, DeltinScript deltinScript)
            {
                _instance = instance;
                _deltinScript = deltinScript;
            }

            // Build the constructor's block.
            public void Build(ActionSet actionSet) => _instance.Provider.Block.Translate(actionSet);

            // Create the parameter handler for the constructor.
            public IParameterHandler CreateParameterHandler(ActionSet actionSet, WorkshopParameter[] providedParameters)
                => new UserFunctionParameterHandler(actionSet, _instance.Parameters, _instance.ParameterVars, providedParameters);
            
            // Create the return handler for the constructor.
            public ReturnHandler GetReturnHandler(ActionSet actionSet) => new ReturnHandler(actionSet);
            
            // Gets the subroutine, or creates it if it does not exist yet.
            public SubroutineCatalogItem GetSubroutine() => _deltinScript.GetComponent<SubroutineCatalog>().GetSubroutine(_instance, () =>
                new SubroutineBuilder(_deltinScript, new() {
                    ContainingType = _instance.Type,
                    Controller = this,
                    RuleName = _instance.Provider.SubroutineName,
                    ElementName = _instance.Type.GetName() + "_constructor",
                    VariableGlobalDefault = true
                }).SetupSubroutine());

            // Unique stack identifier for recursive constructors.
            // This doesn't matter right now since recursive constructors are not supported.
            public object StackIdentifier() => _instance.Provider;
        }
    }
}