using System;   
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse.Lambda
{
    public class PortableLambdaType : CodeType
    {
        public LambdaKind LambdaKind { get; }
        public CodeType[] Parameters { get; }
        public CodeType ReturnType { get; }
        private readonly Scope _scope = new Scope();

        public PortableLambdaType(LambdaKind lambdaType, CodeType[] parameters, CodeType returnType) : base("portable lambda")
        {
            LambdaKind = lambdaType;
            Parameters = parameters;
            ReturnType = returnType;
            _scope.AddNativeMethod(new LambdaInvoke2(this));
        }

        public PortableLambdaType(LambdaKind lambdaType) : this(lambdaType, new CodeType[0], null) {}

        public bool DoesReturnValue() => throw new NotImplementedException();
        public override bool IsConstant() => LambdaKind == LambdaKind.ConstantBlock || LambdaKind == LambdaKind.ConstantMacro || LambdaKind == LambdaKind.ConstantValue;
        public override Scope GetObjectScope() => _scope;

        public override bool Implements(CodeType type)
        {
            var other = type as PortableLambdaType;
            if (other == null || Parameters.Length != other.Parameters.Length) return false;

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
            return true;
        }

        public override CompletionItem GetCompletion() => throw new NotImplementedException();
        public override Scope ReturningScope() => null;
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