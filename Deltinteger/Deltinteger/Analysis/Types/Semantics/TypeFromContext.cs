using DS.Analysis.Scopes;
using DS.Analysis.Diagnostics;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Types.Semantics
{
    static class TypeFromContext
    {
        public static TypeReference TypeReferenceFromContext(ContextInfo context, IParseType typeContext) => TypeReferenceFromContext(context, (ITypeContextHandler)typeContext);

        public static TypeReference TypeReferenceFromContext(ContextInfo context, ITypeContextHandler typeContext)
        {
            var identifierWatcher = context.Scope.Watch();
            var errorHandler = new TypeIdentifierErrorHandler(context.File.Diagnostics, typeContext.Identifier.Text, typeContext.Identifier.Range);

            return new IdentifierTypeReference(errorHandler, identifierWatcher, null);
        }

        class TypeIdentifierErrorHandler : ITypeIdentifierErrorHandler
        {
            readonly FileDiagnostics diagnostics;
            readonly string name;
            readonly DocRange range;
            Diagnostic currentDiagnostic;

            public TypeIdentifierErrorHandler(FileDiagnostics diagnostics, string name, DocRange range)
            {
                this.diagnostics = diagnostics;
                this.name = name;
                this.range = range;
            }

            public void Dispose() => currentDiagnostic.Dispose();

            public void GenericCountMismatch() => SetDiagnostic(Err(Messages.GenericCountMismatch(name, 0, 0)));

            public void NoTypesMatchName() => SetDiagnostic(Err(Messages.TypeNameNotFound(name)));

            public void Success() => SetDiagnostic(null);


            Diagnostic Err(string message) => diagnostics.Error(message, range);

            void SetDiagnostic(Diagnostic newDiagnostic)
            {
                currentDiagnostic?.Dispose();
                currentDiagnostic = newDiagnostic;
            }
        }
    }
}