using System;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Types.Semantics
{
    static class TypeFromContext
    {
        public static IDisposableTypeDirector TypeReferenceFromSyntax(ContextInfo context, IParseType syntax)
        {
            if (syntax is TypeSyntax typeSyntax)
                return new Semantics.TypeTree(context, typeSyntax.Parts);

            throw new NotImplementedException(syntax.GetType().Name);
        }
    }
}