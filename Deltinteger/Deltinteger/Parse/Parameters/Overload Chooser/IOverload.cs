using System;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Parse.Overload
{
    public interface IOverload
    {
        CodeParameter[] Parameters { get; }
        int TypeArgCount { get; }
        StringOrMarkupContent Documentation { get; }
        IParameterCallable Value { get; }
        bool RestrictedValuesAreFatal { get; }
        InstanceAnonymousTypeLinker GetTypeLinker(CodeType[] typeArgs);
        MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo);
    }

    class MethodOverload : IOverload
    {
        public CodeParameter[] Parameters => _function.Parameters;
        public int TypeArgCount => _function.MethodInfo.TypeArgCount;
        public StringOrMarkupContent Documentation => _function.Documentation;
        public IParameterCallable Value => _function;
        public bool RestrictedValuesAreFatal => _function.RestrictedValuesAreFatal;
        private readonly IMethod _function;

        public MethodOverload(IMethod function)
        {
            _function = function;
        }

        public InstanceAnonymousTypeLinker GetTypeLinker(CodeType[] typeArgs) => _function.MethodInfo.GetInstanceInfo(typeArgs);
        public MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo) => _function.GetLabel(deltinScript, labelInfo);
    }

    class ConstructorOverload : IOverload
    {
        public CodeParameter[] Parameters => _constructor.Parameters;
        public int TypeArgCount => 0;
        public StringOrMarkupContent Documentation => _constructor.Documentation;
        public IParameterCallable Value => _constructor;
        public bool RestrictedValuesAreFatal => true;
        private readonly Constructor _constructor;

        public ConstructorOverload(Constructor constructor)
        {
            _constructor = constructor;
        }

        public InstanceAnonymousTypeLinker GetTypeLinker(CodeType[] typeArgs) => throw new NotImplementedException();
        public MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo) => _constructor.GetLabel(deltinScript, labelInfo);
    }
}