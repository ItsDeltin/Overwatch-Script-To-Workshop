using System;
using System.Linq;
using DS.Analysis.Scopes;
using DS.Analysis.Types;
using DS.Analysis.Types.Standard;
using DS.Analysis.Utility;
using DS.Analysis.Diagnostics;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Expressions.Identifiers
{
    class IdentifierExpression : Expression, ITypeDirector, IObservable<Scope>
    {
        readonly ObserverCollection<CodeType> typeObservers = new ObserverCollection<CodeType>();
        readonly ObserverCollection<Scope> scopeObservers = new ObserverCollection<Scope>();
        readonly ScopeWatcher scopeWatcher;
        readonly Token token;
        readonly FileDiagnostics diagnostics;

        Diagnostic currentDiagnostic;
        IDisposable currentTypeSubscription;
        IDisposable currentScopeSubscription;

        public IdentifierExpression(ContextInfo contextInfo, Identifier identifier)
        {
            // Set type and scope handlers.
            Type = this; // ITypeDirector
            Scope = this; // IObservable<Scope>

            this.token = identifier.Token;
            this.diagnostics = contextInfo.File.Diagnostics;

            // Create scope watcher.
            AddDisposable(scopeWatcher = contextInfo.Scope.Watch());

            // Watch changes.
            scopeWatcher.Subscribe(FilterIdentifiers);
        }

        void FilterIdentifiers(ScopeWatcherValue newIdentifiers)
        {
            currentTypeSubscription?.Dispose();
            currentTypeSubscription = null;
            currentScopeSubscription?.Dispose();
            currentScopeSubscription = null;
            currentDiagnostic?.Dispose();
            currentDiagnostic = null;

            var identifier = ChooseIdentifierHandler(newIdentifiers.FoundElements);

            // Subscribe to the identifier's type.
            var typeDirector = identifier.GetTypeDirector();

            if (typeDirector != null)
                // Identifier has a type
                currentTypeSubscription = typeDirector.Subscribe(typeObservers.Set);
            else
                // Type is unknown
                typeObservers.Set(StandardTypes.UnknownInstance);
            
            // Subscribe to the identifier's scope.
            currentScopeSubscription = identifier.GetScopeDirector().Subscribe(scopeObservers.Set);
        }

        IIdentifierHandler ChooseIdentifierHandler(ScopedElementData[] scopedElements)
        {
            foreach (var scopedElement in scopedElements.Where(e => e.IsMatch(token)))
            {
                var identifierHandler = scopedElement.GetIdentifierHandler();
                if (identifierHandler != null)
                    return identifierHandler;
            }

            currentDiagnostic = diagnostics.Error(Messages.IdentifierDoesNotExist(token), token);
            return UnknownIdentifierHandler.Instance;
        }

        // Subscribes to the type being pointed to.
        IDisposable IObservable<CodeType>.Subscribe(IObserver<CodeType> observer) => typeObservers.Add(observer);

        // Subscribes to the scope being pointed to.
        IDisposable IObservable<Scope>.Subscribe(IObserver<Scope> observer) => scopeObservers.Add(observer);

        // Since _currentTypeSubscription may change, we do not want to use 'Node.AddDisposable' as normal.
        // Dispose of it manually.
        public override void Dispose()
        {
            base.Dispose();
            currentTypeSubscription?.Dispose();
            currentScopeSubscription?.Dispose();
            currentDiagnostic?.Dispose();
        }
    }
}