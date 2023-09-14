using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using TypeArgContext = Deltin.Deltinteger.Compiler.SyntaxTree.TypeArgContext;

namespace Deltin.Deltinteger.Parse
{
    public class AnonymousType : CodeType, IDeclarationKey
    {
        ITypeArgTrackee _context;
        public ITypeArgTrackee Context
        {
            get => _context; set
            {
                if (_context == null)
                    _context = value;
                else
                    throw new Exception("AnonymousType context already set");
            }
        }
        public AnonymousTypeAttributes AnonymousTypeAttributes { get; }

        public AnonymousType(string name, AnonymousTypeAttributes attributes) : base(name)
        {
            AnonymousTypeAttributes = attributes;
            Attributes.ContainsGenerics = true;
            ArrayHandler = new AnonymousArrayHandler(attributes.Single);
            Operations.AddAssignmentOperator();
        }

        public override IGenericUsage GetGenericUsage() => new BridgeAnonymousUsage(this);

        public override CodeType GetRealType(InstanceAnonymousTypeLinker instanceInfo) => instanceInfo != null && instanceInfo.Links.TryGetValue(this, out CodeType result) ? result : this;

        public override CompletionItem GetCompletion() => new CompletionItem()
        {
            Label = Name,
            Kind = CompletionItemKind.TypeParameter
        };

        public override AnonymousType[] ExtractAnonymousTypes() => new[] { this };

        public override bool Implements(CodeType type)
        {
            if (AnonymousTypeAttributes.Single && type.Attributes.StackLength == 1)
                return true;

            foreach (var union in type.UnionTypes())
                if (Object.ReferenceEquals(union, this))
                    return true;

            return false;
        }

        public override string GetName(GetTypeName settings = default(GetTypeName))
        {
            if (settings.MakeAnonymousTypesUnknown && !settings.TypeLinker.Links.ContainsKey(this)) return "unknown";
            return base.GetName();
        }

        // Override calls to add a link to the declaration.
        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            base.Call(parseInfo, callRange);
            parseInfo.Script.Elements.AddDeclarationCall(this, new DeclarationCall(callRange, false));
        }

        // Returns the name of the type as it was declared.
        public string GetDeclarationName()
        {
            string result = Name;

            if (AnonymousTypeAttributes.Single)
                result = "single " + Name;

            return result;
        }

        public static AnonymousType[] GetGenerics(ParseInfo parseInfo, List<TypeArgContext> typeArgs, ITypeArgTrackee context)
        {
            var generics = new AnonymousType[typeArgs.Count];
            for (int i = 0; i < typeArgs.Count; i++)
            {
                var anonymousType = new AnonymousType(typeArgs[i].Identifier.GetText(), new(single: typeArgs[i].Single));
                anonymousType.Context = context;

                // Add the declaration call.
                parseInfo.Script.Elements.AddDeclarationCall(anonymousType, new DeclarationCall(typeArgs[i].Identifier.Range, true));

                generics[i] = anonymousType;
            }
            return generics;
        }

        class AnonymousArrayHandler : ITypeArrayHandler
        {
            readonly ArrayFunctionHandler _functionHandler;

            public AnonymousArrayHandler(bool isSingle)
            {
                _functionHandler = new ArrayFunctionHandler(isSingle);
            }

            public IGettableAssigner GetArrayAssigner(AssigningAttributes attributes) => new DataTypeAssigner(attributes);
            public ArrayFunctionHandler GetFunctionHandler() => _functionHandler;
            public void OverrideArray(ArrayType array) { }
        }
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