using System;
using DS.Analysis.Scopes;
using DS.Analysis.Types;

namespace DS.Analysis.Structure
{
    abstract class AbstractDeclaredElement : IDisposable
    {
        public string Name { get; protected set; }


        public abstract void AddToContent(TypeContentBuilder contentBuilder);

        public virtual void AddToScope(IScopeAppender scopeAppender)
        {
            scopeAppender.AddScopedElement(new ScopedElement(Name));
        }


        public virtual void Dispose() { }
    }

    abstract class ParentedDeclaredElement : AbstractDeclaredElement
    {
        public AbstractDeclaredElement[] DeclaredElements { get; protected set; }


        public override void Dispose()
        {
            foreach (var element in DeclaredElements)
                element.Dispose();
        }
    }

    abstract class TypeDeclaredElement : AbstractDeclaredElement
    {
        public IDisposableTypeDirector Type { get; protected set; }

        public override void Dispose()
        {
            base.Dispose();
            Type.Dispose();
        }
    }
}