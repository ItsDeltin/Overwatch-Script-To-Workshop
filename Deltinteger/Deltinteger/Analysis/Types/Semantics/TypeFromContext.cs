using System.Linq;
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
            var typeName = typeContext.Identifier.Text;

            // Create the scope watcher and error handler.
            var identifierWatcher = context.Scope.Watch();
            var errorHandler = new TypeIdentifierErrorHandler(context.File.Diagnostics, typeName, typeContext.Identifier.Range);

            // Get the type args
            var typeArgReferences = typeContext.TypeArgs.Select(typeArg => TypeReferenceFromContext(context, typeArg)).ToArray();

            return new IdentifierTypeReference(typeName, errorHandler, identifierWatcher, typeArgReferences);
        }

        class TypeIdentifierErrorHandler : ITypeIdentifierErrorHandler
        {
            readonly FileDiagnostics diagnostics;
            readonly string referenceName;
            readonly DocRange range;
            Diagnostic currentDiagnostic;

            public TypeIdentifierErrorHandler(FileDiagnostics diagnostics, string name, DocRange range)
            {
                this.diagnostics = diagnostics;
                this.referenceName = name;
                this.range = range;
            }

            public void Dispose() => currentDiagnostic?.Dispose();

            public void GenericCountMismatch(string typeName, int expected) => SetDiagnostic(Err(Messages.GenericCountMismatch(typeName, 0, expected)));

            public void NoTypesMatchName() => SetDiagnostic(Err(Messages.TypeNameNotFound(referenceName)));

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