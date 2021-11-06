using System;
using DS.Analysis.Types;
using DS.Analysis.Scopes;

namespace DS.Analysis.Structure.TypeAlias
{
    class DeclaredTypeAlias : AbstractDeclaredElement
    {
        readonly ITypeAliasProvider provider;
        TypeReference aliasing;

        public DeclaredTypeAlias(ITypeAliasProvider provider)
        {
            this.provider = provider;
        }

        public override void GetMeta(ContextInfo context)
        {
            aliasing = provider.GetTypeReference(context);
        }

        public override void GetContent()
        {
        }

        // todo!!! :)
        // public override ScopedElement MakeScopedElement(ScopedElementParameters parameters) => new ScopedElement(parameters.Alias ?? Name, );
    }
}