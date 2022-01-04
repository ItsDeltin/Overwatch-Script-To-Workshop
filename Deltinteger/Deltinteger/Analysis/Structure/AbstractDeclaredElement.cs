using System;
using DS.Analysis.Scopes;

namespace DS.Analysis.Structure
{
    abstract class AbstractDeclaredElement : IDisposable
    {
        public string Name { get; protected set; }


        public abstract void AddToContent(TypeContentBuilder contentBuilder);

        public abstract void AddToScope(IScopeAppender scopeAppender);


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
}