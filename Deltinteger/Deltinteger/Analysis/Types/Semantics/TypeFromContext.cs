using System;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Types.Semantics
{
    static class TypeFromContext
    {
        public static IDisposableTypeDirector TypeReferenceFromSyntax(ContextInfo context, IParseType syntax)
        {
            // void
            if (syntax.IsVoid)
                return new EmptyDisposableTypeDirector(StandardType.Void.Director);

            // type tree
            if (syntax is TypeSyntax typeSyntax)
                return Semantics.Path.TypePath.CreateTypeTree(context, typeSyntax.Parts);

            throw new NotImplementedException(syntax.GetType().Name);
        }
    }
}