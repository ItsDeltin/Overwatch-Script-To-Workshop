using System;
using System.Reactive.Disposables;

namespace DS.Analysis.Types.Semantics
{
    using Diagnostics;
    using Scopes;

    static class TypeValidation
    {
        public static IDisposable IsAssignableTo(
            ContextInfo context,
            DiagnosticToken token,
            ScopedElement[] scopedElements,
            CodeType assignToType,
            CodeType valueType)
        {
            // Types are compatible; no error.
            if (valueType.Comparison.CanBeAssignedTo(assignToType))
                return Disposable.Empty;

            // Not assignable.
            return context.PostAnalysisOperations.Add(() =>
            {
                // Create the identifier context.
                var identifierContext = new GetIdentifierContext(scopedElements);

                // Create the error.
                return token.Error(Messages.NotAssignable(
                    valueType.GetIdentifier.PathFromContext(identifierContext),
                    assignToType.GetIdentifier.PathFromContext(identifierContext)));
            });
        }
    }
}