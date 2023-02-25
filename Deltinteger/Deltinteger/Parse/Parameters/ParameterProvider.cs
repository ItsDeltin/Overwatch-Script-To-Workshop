using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public interface IParameterProvider
    {
        ParameterInstance GetInstance(InstanceAnonymousTypeLinker instanceInfo);
    }

    public class ParameterInstance
    {
        public CodeParameter Parameter { get; }
        public IVariableInstance Variable { get; }

        public ParameterInstance(CodeParameter parameter, IVariableInstance variableInstance)
        {
            Parameter = parameter;
            Variable = variableInstance;
        }
    }

    public class ParameterProvider : IParameterProvider, IRestrictedCallHandler, IParameterLike
    {
        public string Name { get; }
        public Var Var { get; private set; }
        public CodeType Type { get; private set; }
        public ExpressionOrWorkshopValue DefaultValue { get; private set; }
        public ParameterAttributes Attributes { get; private set; }
        public MarkupBuilder Description { get; private set; }
        private readonly ParameterInvokedInfo _invoked = new ParameterInvokedInfo();
        private readonly List<RestrictedCallType> _restrictedCalls = new List<RestrictedCallType>();

        public ParameterProvider(string name)
        {
            Name = name;
        }

        public ParameterInstance GetInstance(InstanceAnonymousTypeLinker instanceInfo) => new ParameterInstance(new CodeParameter(Name, Description, Type.GetRealType(instanceInfo), DefaultValue)
        {
            Attributes = Attributes,
            Invoked = _invoked,
            RestrictedCalls = _restrictedCalls,
            DefaultValue = DefaultValue
        }, Var.GetInstance(null, instanceInfo));

        public void AddRestrictedCall(RestrictedCall restrictedCall)
        {
            if (!_restrictedCalls.Contains(restrictedCall.CallType))
                _restrictedCalls.Add(restrictedCall.CallType);
        }

        public static ParameterProvider[] GetParameterProviders(ParseInfo parseInfo, Scope methodScope, List<VariableDeclaration> context, bool subroutineParameter, ParsedMetaComment metaComment = null)
        {
            if (context == null) return new ParameterProvider[0];

            var parameters = new ParameterProvider[context.Count];
            for (int i = 0; i < parameters.Length; i++)
            {
                Var newVar;
                string name = context[i].Identifier.GetText();
                var documentation = metaComment?.GetParameterDescription(name);

                ParameterProvider parameter = new ParameterProvider(name);

                // Set up the context handler.
                IVarContextHandler contextHandler = new DefineContextHandler(parseInfo.SetRestrictedCallHandler(parameter), context[i], documentation);

                // Normal parameter
                if (!subroutineParameter)
                    newVar = (Var)new ParameterVariable(methodScope, contextHandler, parameter._invoked).GetVar();
                // Subroutine parameter.
                else
                    newVar = (Var)new SubroutineParameterVariable(methodScope, contextHandler).GetVar();

                parameter.Var = newVar;
                parameter.Type = newVar.CodeType;
                parameter.Attributes = new ParameterAttributes(newVar.Ref, newVar.VariableType == VariableType.ElementReference);
                parameter.Description = documentation;

                if (newVar.InitialValue != null) parameter.DefaultValue = new ExpressionOrWorkshopValue(newVar.InitialValue);

                parameters[i] = parameter;
            }

            return parameters;
        }

        public string GetLabel(DeltinScript deltinScript, AnonymousLabelInfo labelInfo)
        {
            string result = string.Empty;

            if (Attributes.Ref) result = "ref ";
            else if (Attributes.In) result = "in ";

            result += Type.GetName() + " " + Name;
            if (DefaultValue != null) result = "[" + result + "]";
            return result;
        }
    }
}