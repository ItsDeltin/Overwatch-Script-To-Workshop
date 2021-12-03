using System;
using System.Reactive.Disposables;

namespace DS.Analysis.Types.Semantics
{
    using Diagnostics;
    using Utility;

    static class TypeValidation
    {
        public static IDisposable IsAssignableTo(ContextInfo context, DiagnosticToken token, ITypeDirector assignToType, ITypeDirector valueType)
        {
            var watcher = context.Scope.Watch();
            return new CompositeDisposable(new[] {
                watcher,
                Helper.Observe(watcher, assignToType, valueType, (scopeInfo, assignToType, valueType) =>
                {
                    // Types are compatible; no error.
                    if (valueType.Comparison.CanBeAssignedTo(assignToType))
                        return Disposable.Empty;

                    // Not assignable.
                    return context.PostAnalysisOperations.Add(() => {
                        // Create the identifier context.
                        var identifierContext = new GetIdentifierContext(scopeInfo.Elements);

                        // Create the error.
                        return token.Error(Messages.NotAssignable(
                            valueType.GetIdentifier.PathFromContext(identifierContext),
                            assignToType.GetIdentifier.PathFromContext(identifierContext)));
                    });
                })
            });
        }
    }
}