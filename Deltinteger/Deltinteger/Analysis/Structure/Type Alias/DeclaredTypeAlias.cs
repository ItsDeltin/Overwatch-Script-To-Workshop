using System;
using DS.Analysis.Types;
using DS.Analysis.Scopes;

namespace DS.Analysis.Structure.TypeAlias
{
    class DeclaredTypeAlias : AbstractDeclaredElement
    {
        readonly ITypeAliasProvider provider;
        TypeReference aliasing;

        public DeclaredTypeAlias(ContextInfo context, ITypeAliasProvider provider)
        {
            this.provider = provider;
            aliasing = provider.GetTypeReference(context);
        }

        // todo!!! :)
        // public override ScopedElement MakeScopedElement(ScopedElementParameters parameters) => new ScopedElement(parameters.Alias ?? Name, );
    }
}