using System;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Parse
{
    public interface ICodeTypeInitializer : ITypeArgTrackee
    {
        string Name { get; }
        CodeType GetInstance();
        CodeType GetInstance(GetInstanceInfo instanceInfo);
        bool BuiltInTypeMatches(Type type);
        CompletionItem GetCompletion();
    }

    class GenericCodeTypeInitializer : ICodeTypeInitializer
    {
        public string Name => _type.Name;
        public int GenericsCount => 0;
        private readonly CodeType _type;

        public GenericCodeTypeInitializer(CodeType type)
        {
            _type = type;
        }

        public CodeType GetInstance() => _type;
        public CodeType GetInstance(GetInstanceInfo instanceInfo) => _type;
        public bool BuiltInTypeMatches(Type type) => _type.GetType() == type;
        public CompletionItem GetCompletion() => _type.GetCompletion();
    }

    public class GetInstanceInfo
    {
        public CodeType[] Generics { get; }

        public GetInstanceInfo(params CodeType[] generics)
        {
            Generics = generics;
        }
    }
}