using System;
using System.Linq;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse.Lambda
{
    public class PortableLambdaType : CodeType
    {
        public LambdaKind LambdaKind { get; }
        public CodeType[] Parameters { get; }
        public CodeType ReturnType { get; protected set; }
        public bool ReturnsValue { get; protected set; }
        public bool ParameterTypesKnown { get; }
        public LambdaInvoke InvokeFunction { get; private set; }

        readonly Scope _scope = new Scope();

        public PortableLambdaType(PortableLambdaTypeBuilder builder) : base(builder.Name)
        {
            LambdaKind = builder.LambdaKind;
            Parameters = builder.Parameters;
            ReturnType = builder.ReturnType;
            ReturnsValue = builder.ReturnsValue;
            ParameterTypesKnown = builder.ParameterTypesKnown;
            NeedsArrayProtection = true;

            Attributes.ContainsGenerics = (Parameters?.Any(p => p.Attributes.ContainsGenerics) ?? false) || (ReturnsValue && ReturnType.Attributes.ContainsGenerics);

            // Make the lambda assignable if the LambdaKind is compatible.
            if (LambdaKind == LambdaKind.Portable)
                Operations.AddAssignmentOperator();

            // Set Generics.
            IEnumerable<CodeType> generics = Enumerable.Empty<CodeType>();
            if (Parameters != null) generics = generics.Concat(Parameters);
            if (ReturnType != null) generics = generics.Append(ReturnType);
            Generics = generics.ToArray();

            // Add the invoke function.
            AddInvokeFunction();
        }

        private void AddInvokeFunction()
        {
            InvokeFunction = new LambdaInvoke(this);
            _scope.AddNativeMethod(InvokeFunction);
            InvokeInfo = new LambdaInvokeInfo(this);
        }

        public override bool IsConstant() => LambdaKind == LambdaKind.Constant;
        public override Scope GetObjectScope() => _scope;

        protected override bool DoesImplement(CodeType type)
        {
            var other = type as PortableLambdaType;
            if (other == null || Parameters.Length != other.Parameters.Length) return false;

            if (ParameterTypesKnown)
                // Make sure the parameters match.
                for (int i = 0; i < Parameters.Length; i++)
                {
                    if (Parameters[i] == null)
                    {
                        if (other.Parameters[i] != null && other.Parameters[i].IsConstant())
                            return false;
                    }
                    else if (!Parameters[i].Implements(other.Parameters[i]))
                        return false;
                }

            // Make sure the return type matches.
            return other.ReturnsValue == ReturnsValue && (!ReturnsValue || ReturnType.Implements(other.ReturnType));
        }

        public override CodeType GetRealType(InstanceAnonymousTypeLinker instanceInfo)
        {
            if (!Attributes.ContainsGenerics)
                return this;

            return new PortableLambdaType(new PortableLambdaTypeBuilder(
                kind: LambdaKind,
                name: Name,
                parameters: Parameters.Select(p => p.GetRealType(instanceInfo)).ToArray(),
                returnType: ReturnType?.GetRealType(instanceInfo),
                returnsValue: ReturnsValue,
                parameterTypesKnown: ParameterTypesKnown));
        }

        public override string GetName(GetTypeName settings = default(GetTypeName))
        {
            string result = string.Empty;

            // Constant
            if (LambdaKind == LambdaKind.Constant)
                result += "const ";

            // Single parameter
            if (Parameters.Length == 1)
                result += Parameters[0].GetName(settings);
            else // Zero or more than one parameter.
            {
                result += "(";
                for (int i = 0; i < Parameters.Length; i++)
                {
                    result += Parameters[i].GetName(settings);
                    if (i < Parameters.Length - 1) result += ", ";
                }
                result += ")";
            }

            result += " => ";

            // Void
            if (!ReturnsValue) result += "void";
            else result += ReturnType.GetName(settings);

            return result;
        }

        public override IEnumerable<CodeType> GetAssigningTypes() => Enumerable.Empty<CodeType>();

        public static PortableLambdaType CreateConstantType(CodeType returnType = null, params CodeType[] parameterTypes) =>
            new PortableLambdaType(new(
                kind: LambdaKind.Constant,
                parameters: parameterTypes,
                returnType: returnType,
                parameterTypesKnown: true,
                returnsValue: returnType != null
            ));
    }

    public struct PortableLambdaTypeBuilder
    {
        public string Name { get; } // The name of the CodeType.
        public LambdaKind LambdaKind { get; } // The type of lambda this is.
        public CodeType[] Parameters { get; } // The parameter types of the lambda.
        public CodeType ReturnType { get; } // The return type of the lambda.

        public bool ReturnsValue { get; } // Does the lambda return a value? May be true when ReturnType is null.
        public bool ParameterTypesKnown { get; } // Are parameter types known?

        public PortableLambdaTypeBuilder(LambdaKind kind, CodeType[] parameters, CodeType returnType)
        {
            Name = "lambda";
            LambdaKind = kind;
            Parameters = parameters;
            ReturnType = returnType;
            ReturnsValue = returnType != null;
            ParameterTypesKnown = true;
        }

        public PortableLambdaTypeBuilder(
            LambdaKind kind,
            string name = "lambda",
            CodeType[] parameters = null,
            CodeType returnType = null,
            bool returnsValue = false,
            bool parameterTypesKnown = false)
        {
            Name = name;
            LambdaKind = kind;
            Parameters = parameters ?? new CodeType[0];
            ReturnType = returnType;
            ReturnsValue = returnsValue;
            ParameterTypesKnown = parameterTypesKnown;
        }
    }

    class UnknownLambdaType : CodeType
    {
        public int ArgumentCount { get; }

        public UnknownLambdaType(int argumentCount) : base(null)
        {
            ArgumentCount = argumentCount;
        }

        public override CompletionItem GetCompletion() => throw new NotImplementedException();
        public override Scope ReturningScope() => throw new NotImplementedException();
    }

    public enum LambdaKind
    {
        Anonymous,
        Portable,
        Constant
    }
}