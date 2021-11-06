using DS.Analysis.Types;
using DS.Analysis.Types.Semantics;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Structure.TypeAlias
{
    interface ITypeAliasProvider
    {
        TypeReference GetTypeReference(ContextInfo context);
    }

    class TypeAliasProvider : ITypeAliasProvider
    {
        readonly TypeAliasContext typeAliasSyntax;

        public TypeAliasProvider(TypeAliasContext typeAliasSyntax)
        {
            this.typeAliasSyntax = typeAliasSyntax;
        }

        public TypeReference GetTypeReference(ContextInfo context) => TypeFromContext.TypeReferenceFromContext(context, typeAliasSyntax.OtherType);
    }
}