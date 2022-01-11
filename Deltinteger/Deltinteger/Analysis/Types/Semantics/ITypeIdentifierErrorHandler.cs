using System;
using System.Reactive;
using System.Reactive.Disposables;

namespace DS.Analysis.Types.Semantics
{
    using Diagnostics;
    using Scopes;

    interface ITypeIdentifierErrorHandler : IDisposable
    {
        void Clear();
        void NoTypesMatchName();
        void GenericCountMismatch(CodeTypeProvider provider, int expected);
        void ModuleHasTypeArgs();
    }


    class TypeIdentifierErrorHandler : ITypeIdentifierErrorHandler
    {
        readonly ContextInfo context;
        readonly NamedDiagnosticToken token;

        IDisposable currentDiagnostic;

        public TypeIdentifierErrorHandler(ContextInfo context, NamedDiagnosticToken token)
        {
            this.context = context;
            this.token = token;
        }

        public void Dispose() => currentDiagnostic?.Dispose();

        public void GenericCountMismatch(CodeTypeProvider provider, int expected) =>
            SetDiagnostic(
                // Add the error after analyzation.
                context.PostAnalysisOperations.Add(() =>
                {
                    SerialDisposable error = new SerialDisposable();
                    return new CompositeDisposable() {
                        error,
                        // Watch the current scope.
                        context.Scope.WatchAndSubscribe(Observer.Create<ScopeSourceChange>(
                            // Add the error.
                            change => error.Disposable = token.Error(Messages.GenericCountMismatch(
                                // Extract the type name.
                                typeName: provider.GetIdentifier.PathFromContext(new GetIdentifierContext(change.Elements)),
                                provided: 0,
                                expected: expected))))
                    };
                }));

        public void ModuleHasTypeArgs() => SetDiagnostic(token.Error(Messages.ModuleHasTypeArgs()));

        public void NoTypesMatchName() => SetDiagnostic(token.Error(name => Messages.TypeNameNotFound(name)));

        public void Clear() => SetDiagnostic(null);


        void SetDiagnostic(IDisposable newDiagnostic)
        {
            currentDiagnostic?.Dispose();
            currentDiagnostic = newDiagnostic;
        }
    }
}