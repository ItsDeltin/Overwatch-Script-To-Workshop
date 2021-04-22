using System;
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
        protected readonly Scope _scope = new Scope();

        public PortableLambdaType(LambdaKind lambdaType, CodeType[] parameters, bool returnsValue, CodeType returnType, bool parameterTypesKnown) : base("lambda")
        {
            LambdaKind = lambdaType;
            Parameters = parameters;
            ReturnsValue = returnsValue;
            ReturnType = returnType;
            ParameterTypesKnown = parameterTypesKnown;
            AddInvokeFunction();
        }

        public PortableLambdaType(LambdaKind lambdaType) : this(lambdaType, new CodeType[0], false, null, false) { }

        protected PortableLambdaType(string name, LambdaKind lambdaKind, CodeType[] parameters) : base(name)
        {
            LambdaKind = lambdaKind;
            ParameterTypesKnown = true;
            Parameters = parameters;
            AddInvokeFunction();
        }

        private void AddInvokeFunction()
        {
            InvokeFunction = new LambdaInvoke(this);
            _scope.AddNativeMethod(InvokeFunction);
            InvokeInfo = new LambdaInvokeInfo(this);
        }

        public override bool IsConstant() => LambdaKind == LambdaKind.ConstantBlock || LambdaKind == LambdaKind.ConstantMacro || LambdaKind == LambdaKind.ConstantValue;
        public override Scope GetObjectScope() => _scope;

        public override bool Implements(CodeType type)
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
            return other.ReturnsValue == ReturnsValue && (((ReturnType == null) == (other.ReturnType == null)) || (ReturnType != null && ReturnType.Implements(other.ReturnType)));
        }

        public override CompletionItem GetCompletion() => throw new NotImplementedException();
        public override Scope ReturningScope() => null;

        public override string GetName()
        {
            string result = string.Empty;

            // Single parameter
            if (Parameters.Length == 1)
                result += Parameters[0]?.GetName() ?? "define";
            else // Zero or more than one parameter.
            {
                result += "(";
                for (int i = 0; i < Parameters.Length; i++)
                {
                    result += Parameters[i]?.GetName() ?? "define";
                    if (i < Parameters.Length - 1) result += ", ";
                }
                result += ")";
            }

            result += " => ";

            // Void
            if (!ReturnsValue) result += "void";
            else result += ReturnType?.GetName() ?? "define";

            return result;
        }
    }

    public enum LambdaKind
    {
        Anonymous,
        Portable,
        ConstantBlock,
        ConstantValue,
        ConstantMacro
    }
}