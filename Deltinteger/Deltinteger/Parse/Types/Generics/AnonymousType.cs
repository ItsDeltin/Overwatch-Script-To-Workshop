using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class AnonymousType : CodeType
    {
        public AnonymousType(string name) : base(name) {}
        
        public override CodeType GetRealType(InstanceAnonymousTypeLinker instanceInfo) => instanceInfo != null && instanceInfo.Links.TryGetValue(this, out CodeType result) ? result : this;

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.TypeParameter
        };

        public override AnonymousType[] ExtractAnonymousTypes() => new[] { this };

        public static AnonymousType[] GetGenerics(List<Token> typeArgs)
        {
            var generics = new AnonymousType[typeArgs.Count];
            for (int i = 0; i < typeArgs.Count; i++)
            {
                var anonymousType = new AnonymousType(typeArgs[i].GetText());
                generics[i] = anonymousType;
            }
            return generics;
        }
    }
}