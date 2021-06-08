using System;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public interface IClassInitializer : ITypeArgTrackee
    {
        ClassType Extends { get; }
        IGetMeta MetaGetter { get; }
    }

    public abstract class ClassInitializer : ICodeTypeInitializer, IClassInitializer
    {
        public string Name { get; }
        public AnonymousType[] GenericTypes { get; protected set; }
        public int GenericsCount => GenericTypes.Length;
        public CodeType WorkingInstance { get; protected set; }
        public ClassType Extends { get; protected set; }
        public IGetMeta MetaGetter { get; protected set; }

        public ClassInitializer(string name)
        {
            Name = name;
        }

        public abstract bool BuiltInTypeMatches(Type type);
        public virtual CodeType GetInstance() => new ClassType(Name);
        public virtual CodeType GetInstance(GetInstanceInfo instanceInfo) => new ClassType(Name);

        public CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Class
        };
    }
}