using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public interface IParameterProvider
    {
        CodeParameter GetInstance(InstanceAnonymousTypeLinker instanceInfo);
    }

    public class ParameterProvider : IParameterProvider, IRestrictedCallHandler
    {
        public string Name { get; }
        public Var Var { get; private set; }
        public CodeType Type { get; private set; }
        public ExpressionOrWorkshopValue DefaultValue { get; private set; }
        private readonly ParameterInvokedInfo _invoked = new ParameterInvokedInfo();
        private readonly List<RestrictedCallType> _restrictedCalls = new List<RestrictedCallType>();

        public ParameterProvider(string name)
        {
            Name = name;
        }

        public CodeParameter GetInstance(InstanceAnonymousTypeLinker instanceInfo) => new CodeParameter(Name, Type.GetRealerType(instanceInfo), DefaultValue) {
            Invoked = _invoked,
            RestrictedCalls = _restrictedCalls
        };

        public void RestrictedCall(RestrictedCall restrictedCall)
        {
            if (!_restrictedCalls.Contains(restrictedCall.CallType))
                _restrictedCalls.Add(restrictedCall.CallType);
        }

        public static ParameterProvider[] GetParameterProviders(ParseInfo parseInfo, Scope methodScope, List<VariableDeclaration> context, bool subroutineParameter)
        {
            if (context == null) return new ParameterProvider[0];

            var parameters = new ParameterProvider[context.Count];
            for (int i = 0; i < parameters.Length; i++)
            {
                Var newVar;

                ParameterProvider parameter = new ParameterProvider(context[i].Identifier.GetText());

                // Set up the context handler.
                IVarContextHandler contextHandler = new DefineContextHandler(parseInfo.SetRestrictedCallHandler(parameter), context[i]);

                // Normal parameter
                if (!subroutineParameter)
                    newVar = new ParameterVariable(methodScope, contextHandler, parameter._invoked);
                // Subroutine parameter.
                else
                    newVar = new SubroutineParameterVariable(methodScope, contextHandler);

                parameter.Var = newVar;
                parameter.Type = newVar.CodeType;

                if (newVar.InitialValue != null) parameter.DefaultValue = new ExpressionOrWorkshopValue(newVar.InitialValue);

                parameters[i] = parameter;
            }

            return parameters;
        }
    }
}