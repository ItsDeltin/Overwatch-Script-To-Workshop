using Deltin.Deltinteger.Parse.FunctionBuilder;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse.Types.Constructors
{
    public class DefinedConstructorInstance : Constructor
    {
        public DefinedConstructorProvider Provider { get; }
        public IVariableInstance[] ParameterVars { get; }

        public DefinedConstructorInstance(
            DefinedConstructorProvider provider,
            InstanceAnonymousTypeLinker genericsLinker,
            Location definedAt,
            AccessLevel accessLevel
        ) : base(provider.Type, definedAt, accessLevel)
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

        public override void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            var builder = new FunctionBuildController(actionSet.PackThis(), new CallHandler(parameterValues), new ConstructorDeterminer(this));
            builder.Call();
        }

        /*
        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            parseInfo.CurrentCallInfo?.Call(_recursiveCallHandler, callRange);
            Type.AddLink(parseInfo.GetLocation(callRange));
        }
        */
    }
}