using System;
using DS.Analysis.Scopes;
using DS.Analysis.Types;

namespace DS.Analysis.Structure
{
    abstract class AbstractDeclaredElement : IDisposable
    {
        public string Name { get; protected set; }

        public virtual void GetMeta(ContextInfo metaContext) {}
        public abstract void GetContent(ContextInfo context);

        public virtual ScopedElement MakeScopedElement(ScopedElementParameters parameters) => new ScopedElement(parameters.Alias ?? Name);

        public ScopedElement MakeScopedElement() => MakeScopedElement(default(ScopedElementParameters));

        public virtual void Dispose() {}
    }

    abstract class ParentedDeclaredElement : AbstractDeclaredElement
    {
        public AbstractDeclaredElement[] DeclaredElements { get; protected set; }

        public override void GetMeta(ContextInfo metaContext)
        {
            foreach (var element in DeclaredElements)
                element.GetMeta(metaContext);
        }

        public override void GetContent(ContextInfo context)
        {
            foreach (var element in DeclaredElements)
                element.GetContent(context);
        }
    }

    abstract class TypeDeclaredElement : AbstractDeclaredElement
    {
        public TypeReference Type { get; protected set; }

        public override void Dispose()
        {
            base.Dispose();
            Type.Dispose();
        }
    }
}