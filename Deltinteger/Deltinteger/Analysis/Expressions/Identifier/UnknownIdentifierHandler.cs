using System;
using System.Reactive.Disposables;
using DS.Analysis.Scopes;
using DS.Analysis.Types;
using DS.Analysis.Types.Standard;

namespace DS.Analysis.Expressions.Identifiers
{
    class UnknownIdentifierHandler : IIdentifierHandler, ITypeDirector, IObservable<Scope>
    {
        public static UnknownIdentifierHandler Instance { get; } = new UnknownIdentifierHandler();

        private UnknownIdentifierHandler() {}

        public ITypeDirector GetTypeDirector() => this;
        public IObservable<Scope> GetScopeDirector() => this;

        // ITypeDirector
        public IDisposable Subscribe(IObserver<CodeType> observer)
        {
            observer.OnNext(StandardTypeProviders.UnknownInstance);
            return Disposable.Empty;
        }

        // IObservable<Scope>
        public IDisposable Subscribe(IObserver<Scope> observer)
        {
            observer.OnNext(Scope.Empty);
            return Disposable.Empty;
        }
    }
}