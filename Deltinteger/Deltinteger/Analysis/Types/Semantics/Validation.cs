using System;
using System.Reactive.Disposables;

namespace DS.Analysis.Types.Semantics
{
    using Diagnostics;
    using static Utility.ObserverGroup;

    static class TypeValidation
    {
        public static IDisposable IsAssignableTo(DiagnosticToken token, CodeType assignToType, CodeType valueType)
        {
            // Types are compatible; no error.
            if (valueType.Comparison.CanBeAssignedTo(assignToType))
                return Disposable.Empty;

            return token.Error("Not assignable");
        }

        public static IDisposable IsAssignableTo(DiagnosticToken token, ITypeDirector assignToType, ITypeDirector valueType)
            => Observe(assignToType, valueType, (a, b) => IsAssignableTo(token, a, b));


        public static IDisposable IsAssignableTo(ContextInfo context, ITypeDirector assignToType, ITypeDirector valueType)
        {
            var watcher = context.Scope.Watch();
            return new CompositeDisposable(new[] {
                watcher,
                Observe(watcher, assignToType, valueType, (scopeInfo, assignToType, valueType) =>
                {
                    // Types are compatible; no error.
                    if (valueType.Comparison.CanBeAssignedTo(assignToType))
                        return Disposable.Empty;

                    return null;
                })
            });
        }
    }
}