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
        void GenericCountMismatch(IGetIdentifier typeIdentifier, int expected);
        void ModuleHasTypeArgs();
        void GotModuleExpectedType();
        bool HasError();
    }


    class TypeIdentifierErrorHandler : ITypeIdentifierErrorHandler
    {
        readonly ContextInfo context;
        readonly NamedDiagnosticToken token;

        IDisposable currentDiagnostic;
        bool disposed;

        public TypeIdentifierErrorHandler(ContextInfo context, NamedDiagnosticToken token)
        {
            this.context = context;
            this.token = token;
        }

        public void Dispose()
        {
            if (disposed)
                throw new ObjectDisposedException(ToString());
            disposed = true;

            currentDiagnostic?.Dispose();
        }

        public void GenericCountMismatch(IGetIdentifier typeIdentifier, int expected) =>
            SetDiagnostic(
                // Add the error after analyzation.
                context.PostAnalysisOperations.Add(() =>
                {
                    return token.Error(Messages.GenericCountMismatch(
                        // Extract the type name.
                        typeName: typeIdentifier.PathFromContext(new GetIdentifierContext(context.Scope.Elements)),
                        provided: 0,
                        expected: expected));
                    // SerialDisposable error = new SerialDisposable();
                    // return new CompositeDisposable() {
                    //     error,
                    //     // Watch the current scope.
                    //     context.Scope.WatchAndSubscribe(Observer.Create<ScopeSourceChange>(
                    //         // Add the error.
                    //         change => error.Disposable = token.Error(Messages.GenericCountMismatch(
                    //             // Extract the type name.
                    //             typeName: provider.GetIdentifier.PathFromContext(new GetIdentifierContext(change.Elements)),
                    //             provided: 0,
                    //             expected: expected))))
                    // };
                }));

        public void ModuleHasTypeArgs() => SetDiagnostic(token.Error(Messages.ModuleHasTypeArgs()));

        public void NoTypesMatchName() => SetDiagnostic(token.Error(name => Messages.TypeNameNotFound(name)));

        public void GotModuleExpectedType() => SetDiagnostic(token.Error(name => Messages.GotModuleExpectedType(name)));

        public void Clear() => SetDiagnostic(null);

        public bool HasError() => currentDiagnostic != null;


        void SetDiagnostic(IDisposable newDiagnostic)
        {
            currentDiagnostic?.Dispose();
            currentDiagnostic = newDiagnostic;
        }
    }
}