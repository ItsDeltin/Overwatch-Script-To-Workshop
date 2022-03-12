using System.Linq;
using System.Collections.Generic;
using DS.Analysis.Scopes;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Types.Generics
{
    class TypeArgCollection
    {
        public static readonly TypeArgCollection Empty = new TypeArgCollection(new TypeArg[0]);

        public TypeArg[] TypeArgs { get; }
        public int Count => TypeArgs.Length;

        public TypeArgCollection(TypeArg[] typeArgs)
        {
            TypeArgs = typeArgs;
        }

        public void AddToScope(IScopeAppender scopeAppender)
        {
            foreach (var typeArg in TypeArgs)
                scopeAppender.AddScopedElement(typeArg.ScopedElement);
        }

        public CodeType[] GetTypeArgInstances() => TypeArgs.Select(typeArg => typeArg.DataTypeProvider.Instance).ToArray();

        public static TypeArgCollection FromSyntax(List<TypeArgContext> syntax) => new TypeArgCollection(syntax.Select(g => new TypeArg(g.Identifier.Text, g.Single)).ToArray());
    }
}