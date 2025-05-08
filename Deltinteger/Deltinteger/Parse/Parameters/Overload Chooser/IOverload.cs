using System;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Parse.Overload
{
    public interface IOverload
    {
        CodeParameter[] Parameters { get; }
        AnonymousType[] GenericTypes { get; }
        int TypeArgCount { get; }
        StringOrMarkupContent Documentation { get; }
        IParameterCallable Value { get; }
        ITypeArgTrackee Trackee { get; }
        bool RestrictedValuesAreFatal { get; }
        CodeType ContainingType { get; }
        public AccessLevel AccessLevel { get; }
        CodeType ReturnType { get; }
        InstanceAnonymousTypeLinker GetTypeLinker(CodeType[] typeArgs);
        int TypeArgIndexFromAnonymousType(AnonymousType anonymousType);
        MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo);
    }

    class MethodOverload : IOverload
    {
        public CodeParameter[] Parameters => _function.Parameters;
        public int TypeArgCount => _function.MethodInfo.TypeArgCount;
        public AnonymousType[] GenericTypes => _function.MethodInfo.GenericTypes;
        public StringOrMarkupContent Documentation => _function.Documentation;
        public IParameterCallable Value => _function;
        public ITypeArgTrackee Trackee => _function.MethodInfo.Tracker;
        public bool RestrictedValuesAreFatal => _function.RestrictedValuesAreFatal;
        public CodeType ContainingType => _function.Attributes.ContainingType;
        public AccessLevel AccessLevel => _function.AccessLevel;
        public CodeType ReturnType => _function.CodeType?.GetCodeType(_deltinScript);
        private readonly IMethod _function;
        private readonly DeltinScript _deltinScript;

        public MethodOverload(IMethod function, DeltinScript deltinScript)
        {
            _function = function;
            _deltinScript = deltinScript;
        }

        public InstanceAnonymousTypeLinker GetTypeLinker(CodeType[] typeArgs) => _function.MethodInfo.GetInstanceInfo(typeArgs);
        public int TypeArgIndexFromAnonymousType(AnonymousType anonymousType) => _function.MethodInfo.TypeArgIndexFromAnonymousType(anonymousType);
        public MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo) => _function.GetLabel(deltinScript, labelInfo);
    }

    class ConstructorOverload : IOverload
    {
        public CodeParameter[] Parameters => _constructor.Parameters;
        public int TypeArgCount => 0;
        public StringOrMarkupContent Documentation => _constructor.Documentation;
        public IParameterCallable Value => _constructor;
        public bool RestrictedValuesAreFatal => true;
        public AnonymousType[] GenericTypes => new AnonymousType[0];
        public ITypeArgTrackee Trackee => null;
        public CodeType ContainingType => _constructor.Type;
        public AccessLevel AccessLevel => _constructor.AccessLevel;
        public CodeType ReturnType => null;
        private readonly Constructor _constructor;

        public ConstructorOverload(Constructor constructor)
        {
            _constructor = constructor ?? throw new NullReferenceException(nameof(constructor));
        }

        public InstanceAnonymousTypeLinker GetTypeLinker(CodeType[] typeArgs) => throw new NotImplementedException();
        public int TypeArgIndexFromAnonymousType(AnonymousType anonymousType) => throw new NotImplementedException();
        public MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo) => _constructor.GetLabel(deltinScript, labelInfo);
    }
}