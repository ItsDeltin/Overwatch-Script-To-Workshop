using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using TypeArgContext = Deltin.Deltinteger.Compiler.SyntaxTree.TypeArgContext;

namespace Deltin.Deltinteger.Parse
{
    public class AnonymousType : CodeType
    {
        public AnonymousTypeContext Context { get; }
        public AnonymousTypeAttributes AnonymousTypeAttributes { get; }

        public AnonymousType(string name, AnonymousTypeContext context, AnonymousTypeAttributes attributes) : base(name)
        {
            Context = context;
            AnonymousTypeAttributes = attributes;
            Attributes.ContainsGenerics = true;
        }

        public override IGenericUsage GetGenericUsage() => new BridgeAnonymousUsage(this);
        
        public override CodeType GetRealType(InstanceAnonymousTypeLinker instanceInfo) => instanceInfo != null && instanceInfo.Links.TryGetValue(this, out CodeType result) ? result : this;

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.TypeParameter
        };

        public override AnonymousType[] ExtractAnonymousTypes() => new[] { this };

        public override bool Implements(CodeType type)
        {
            if (AnonymousTypeAttributes.Single && type.Attributes.StackLength == 1)
                return true;

            return Object.ReferenceEquals(type, this);
        }

        public static AnonymousType[] GetGenerics(List<TypeArgContext> typeArgs, AnonymousTypeContext context)
        {
            var generics = new AnonymousType[typeArgs.Count];
            for (int i = 0; i < typeArgs.Count; i++)
            {
                var anonymousType = new AnonymousType(typeArgs[i].Identifier.GetText(), context, new(single: typeArgs[i].Single));
                generics[i] = anonymousType;
            }
            return generics;
        }
    }

    public enum AnonymousTypeContext
    {
        Type,
        Function
    }

    public class AnonymousTypeAttributes
    {
        public bool Single { get; }

        public AnonymousTypeAttributes(bool single)
        {
            Single = single;
        }
    }
}