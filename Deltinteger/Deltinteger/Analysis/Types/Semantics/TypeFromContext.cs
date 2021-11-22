using System;
using System.Linq;
using DS.Analysis.Scopes;
using DS.Analysis.Diagnostics;
using DS.Analysis.Utility;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Types.Semantics
{
    static class TypeFromContext
    {
        public static IDisposableTypeDirector TypeReferenceFromSyntax(ContextInfo context, IParseType syntax)
        {
            if (syntax is TypeSyntax typeSyntax)
                return TypeReferenceFromSyntax(context, typeSyntax);

            if (syntax is INamedType namedType)
                return TypeReferenceFromName(context, namedType);


            throw new NotImplementedException(syntax.GetType().Name);
        }

        static IDisposableTypeDirector TypeReferenceFromSyntax(ContextInfo context, TypeSyntax syntax)
        {
            return new TypeTree(context, syntax.Parts);
        }

        static TypeReference TypeReferenceFromName(ContextInfo context, INamedType syntax)
        {
            var typeName = syntax.Identifier.Text;

            // Create the scope watcher and error handler.
            var identifierWatcher = context.Scope.Watch();
            var errorHandler = new TypeIdentifierErrorHandler(context.File.Diagnostics, typeName, syntax.Identifier.Range);

            // Get the type args
            var typeArgReferences = syntax.TypeArgs.Select(typeArg => TypeReferenceFromSyntax(context, typeArg)).ToArray();

            return new IdentifierTypeReference(typeName, errorHandler, identifierWatcher, typeArgReferences);
        }


        class TypeTree : IDisposableTypeDirector
        {
            readonly TypeSyntax.TypeNamePart[] partSyntaxes;
            readonly TypeReference[] parts;
            readonly IDisposable[] partSubscriptions;
            readonly ObserverCollection<CodeType> observers = new ValueObserverCollection<CodeType>(Standard.StandardTypes.Unknown.Instance);


            public TypeTree(ContextInfo context, TypeSyntax.TypeNamePart[] partSyntaxes)
            {
                this.partSyntaxes = partSyntaxes;
                parts = new TypeReference[partSyntaxes.Length];
                partSubscriptions = new IDisposable[partSubscriptions.Length];

                SubscribeToPartIndex(0, context);
            }


            void SubscribeToPartIndex(int index, ContextInfo context)
            {
                parts[index] = TypeReferenceFromName(context, partSyntaxes[index]);
                partSubscriptions[index] = parts[index].Subscribe(type =>
                {
                    // If this is the last part, notify the observers.
                    if (index == parts.Length - 1)
                        observers.Set(type);
                    // Otherwise, refresh the proceeding parts.
                    else
                    {
                        Dispose(index + 1);
                        SubscribeToPartIndex(index + 1, context);
                    }
                });
            }

            public IDisposable Subscribe(IObserver<CodeType> observer) => observers.Add(observer);

            public void Dispose()
            {
                Dispose(0);
                observers.Complete();
            }


            void Dispose(int startingIndex)
            {
                for (int i = startingIndex; i < parts.Length; i++)
                {
                    partSubscriptions[i]?.Dispose();
                    parts[i]?.Dispose();
                }
            }
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