using System;
using DS.Analysis.Scopes;
using DS.Analysis.Types;
using DS.Analysis.Utility;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Expressions
{
    class IdentifierExpression : Expression, ITypeDirector, IObservable<Scope>
    {
        readonly ObserverCollection<CodeType> _typeObservers = new ObserverCollection<CodeType>();
        readonly ObserverCollection<Scope> _scopeObservers = new ObserverCollection<Scope>();
        readonly ScopeWatcher _scopeWatcher;
        readonly IIdentifierErrorHandler _errorHandler;

        IDisposable _currentTypeSubscription;
        IDisposable _currentScopeSubscription;

        public IdentifierExpression(ContextInfo contextInfo, Identifier identifier, IIdentifierErrorHandler errorHandler)
        {
            // Set type and scope handlers.
            Type = this; // ITypeDirector
            Scope = this; // IObservable<Scope>

            _errorHandler = errorHandler;

            // Create scope watcher.
            AddDisposable(_scopeWatcher = contextInfo.Scope.Watch(identifier.Token.Text));

            // Watch changes.
            _scopeWatcher.Subscribe(FilterIdentifiers);
        }

        void FilterIdentifiers(ScopeWatcherValue newIdentifiers)
        {
            _currentTypeSubscription?.Dispose();
            _currentTypeSubscription = null;
            _currentScopeSubscription?.Dispose();
            _currentScopeSubscription = null;

            // Identifier does not exist.
            if (newIdentifiers.FoundElements.Length == 0)
            {
                // TODO: ? type, NOT NULL!!!
                _typeObservers.Set(null);
                _errorHandler.IdentifierNotFound();
                return;
            }

            var chosen = newIdentifiers.FoundElements[0];
            var identifier = chosen.GetIdentifierHandler();

            // Subscribe to the identifier's type.
            var typeDirector = identifier.GetTypeDirector();

            if (typeDirector != null)
                _currentTypeSubscription = typeDirector.Subscribe(_typeObservers.Set);
            else
                // TODO: ? type, not null, as usual
                _typeObservers.Set(null);
            
            // Subscribe to the identifier's scope.
            _currentScopeSubscription = identifier.GetScopeDirector().Subscribe(_scopeObservers.Set);

            _errorHandler.Success();
        }

        // Subscribes to the type being pointed to.
        IDisposable IObservable<CodeType>.Subscribe(IObserver<CodeType> observer)
        {
            return _typeObservers.Add(observer);
        }

        // Subscribes to the scope being pointed to.
        IDisposable IObservable<Scope>.Subscribe(IObserver<Scope> observer)
        {
            return _scopeObservers.Add(observer);
        }

        // Since _currentTypeSubscription may change, we do not want to use 'Node.AddDisposable' as normal.
        // Dispose of it manually.
        public override void Dispose()
        {
            base.Dispose();
            _currentTypeSubscription?.Dispose();
            _currentScopeSubscription?.Dispose();
        }
    }

    interface IIdentifierHandler
    {
        ITypeDirector GetTypeDirector();
        IObservable<Scope> GetScopeDirector();
    }

    interface IIdentifierErrorHandler
    {
        void Success();
        void IdentifierNotFound();
    }
}