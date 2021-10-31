using DS.Analysis.Scopes;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Types.Semantics
{
    static class TypeFromContext
    {
        public static TypeReference TypeReferenceFromContext(Scope scope, IParseType typeContext) => TypeReferenceFromContext(scope, (ITypeContextHandler)typeContext);

        public static TypeReference TypeReferenceFromContext(Scope scope, ITypeContextHandler typeContext)
        {
            var identifierWatcher = scope.Watch(typeContext.Identifier.Text);

            return new IdentifierTypeReference(identifierWatcher, null);
        }
    }
}