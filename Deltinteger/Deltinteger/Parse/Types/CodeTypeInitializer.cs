using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Parse
{
    public interface ICodeTypeInitializer
    {
        string Name { get; }
        int GenericsCount { get; }
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

    public class InstanceAnonymousTypeLinker
    {
        public Dictionary<AnonymousType, CodeType> Links { get; } = new Dictionary<AnonymousType, CodeType>();

        public InstanceAnonymousTypeLinker(AnonymousType[] typeArgs, CodeType[] generics)
        {
            for (int i = 0; i < typeArgs.Length; i++)
                Links.Add(typeArgs[i], generics[i]);
        }

        public InstanceAnonymousTypeLinker() {}
    }
}