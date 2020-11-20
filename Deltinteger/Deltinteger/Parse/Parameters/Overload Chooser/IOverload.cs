using System;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Parse.Overload
{
    public interface IOverload
    {
        CodeParameter[] Parameters { get; }
        int TypeArgCount { get; }
        StringOrMarkupContent Documentation { get; }
        string Label { get; }
        IParameterCallable Value { get; }
        InstanceAnonymousTypeLinker GetTypeLinker(CodeType[] typeArgs);
    }

    class MethodOverload : IOverload
    {
        public CodeParameter[] Parameters => _function.Parameters;
        public int TypeArgCount => _function.GetProvider().TypeArgCount;
        public StringOrMarkupContent Documentation => _function.Documentation;
        public string Label => _function.GetLabel(true);
        public IParameterCallable Value => _function;
        private readonly IMethod _function;

        public MethodOverload(IMethod function)
        {
            _function = function;
        }

        public InstanceAnonymousTypeLinker GetTypeLinker(CodeType[] typeArgs) => _function.GetProvider().GetInstanceInfo(typeArgs);
    }

    class ConstructorOverload : IOverload
    {
        public CodeParameter[] Parameters => _constructor.Parameters;
        public int TypeArgCount => 0;
        public StringOrMarkupContent Documentation => _constructor.Documentation;
        public string Label => _constructor.GetLabel(true);
        public IParameterCallable Value => _constructor;
        private readonly Constructor _constructor;

        public ConstructorOverload(Constructor constructor)
        {
            _constructor = constructor;
        }

        public InstanceAnonymousTypeLinker GetTypeLinker(CodeType[] typeArgs) => throw new NotImplementedException();
    }
}