using System;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Types.Semantics
{
    using Standard;

    static class TypeFromContext
    {
        public static IDisposableTypeDirector TypeReferenceFromSyntax(ContextInfo context, IParseType syntax)
        {
            // void
            if (syntax.IsVoid)
                return new EmptyDisposableTypeDirector(StandardTypes.Void.Director);

            // type tree
            if (syntax is TypeSyntax typeSyntax)
                return new Semantics.TypeTree(context, typeSyntax.Parts);

            throw new NotImplementedException(syntax.GetType().Name);
        }
    }
}