using System;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public interface IClassInitializer : ITypeArgTrackee
    {
        ClassType Extends { get; }
        IGetMeta MetaGetter { get; }
        IValueSolve OnReady { get; }
        bool DoesExtend(IClassInitializer other);
    }

    public abstract class ClassInitializer : ICodeTypeInitializer, IClassInitializer
    {
        public string Name { get; }
        public AnonymousType[] GenericTypes { get; protected set; }
        public int GenericsCount => GenericTypes.Length;
        public CodeType WorkingInstance { get; protected set; }
        public ClassType Extends { get; protected set; }
        public IGetMeta MetaGetter { get; protected set; }
        public IValueSolve OnReady { get; protected set; } = new ValueSolveSource(true);

        public ClassInitializer(string name)
        {
            Name = name;
        }

        public bool BuiltInTypeMatches(Type type) => false;
        public abstract CodeType GetInstance();
        public abstract CodeType GetInstance(GetInstanceInfo instanceInfo);

        public CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Class
        };

        public bool DoesExtend(IClassInitializer other)
        {
            IClassInitializer current = this;
            while (current != null)
            {
                if (current == other) return true;
                current = current.Extends?.Provider;
            }
            return false;
        }
    }
}