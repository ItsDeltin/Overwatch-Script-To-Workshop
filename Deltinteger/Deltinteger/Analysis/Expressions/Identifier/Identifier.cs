using System;
using System.Linq;
using DS.Analysis.Scopes;
using DS.Analysis.Types;
using DS.Analysis.Types.Standard;
using DS.Analysis.Diagnostics;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Expressions.Identifiers
{
    class IdentifierExpression : Expression
    {
        readonly ScopeWatcher scopeWatcher;
        readonly Token token;
        readonly FileDiagnostics diagnostics;

        Diagnostic currentDiagnostic;
        IDisposable currentTypeSubscription;
        IDisposable currentScopeSubscription;

        CodeType type;
        Scope scope;

        public IdentifierExpression(ContextInfo contextInfo, Identifier identifier)
        {
            this.token = identifier.Token;
            this.diagnostics = contextInfo.File.Diagnostics;

            // Create scope watcher.
            AddDisposable(scopeWatcher = contextInfo.Scope.Watch());

            // Watch changes.
            scopeWatcher.Subscribe(FilterIdentifiers);
        }

        void FilterIdentifiers(ScopeSourceChange newIdentifiers)
        {
            currentTypeSubscription?.Dispose();
            currentTypeSubscription = null;
            currentScopeSubscription?.Dispose();
            currentScopeSubscription = null;
            currentDiagnostic?.Dispose();
            currentDiagnostic = null;

            var element = ChooseScopedElement(newIdentifiers.Elements);

            // Subscribe to the identifier's type.
            var typeDirector = element.IdentifierHandler?.GetTypeDirector();

            if (typeDirector != null)
                // Identifier has a type
                currentTypeSubscription = typeDirector.Subscribe(SetType);
            else
                // Type is unknown
                currentTypeSubscription = StandardTypes.Unknown.Director.Subscribe(SetType);

            // Subscribe to the identifier's scope.
            currentScopeSubscription = element.IdentifierHandler?.GetScopeDirector().Subscribe(SetScope);
        }

        ScopedElement ChooseScopedElement(ScopedElement[] scopedElements)
        {
            foreach (var scopedElement in scopedElements.Where(e => e.Name == token))
                return scopedElement;

            currentDiagnostic = diagnostics.Error(Messages.IdentifierDoesNotExist(token), token);
            return ScopedElement.Unknown(token);
        }

        void SetType(CodeType type)
        {
            this.type = type;
            Update();
        }

        void SetScope(Scope scope)
        {
            this.scope = scope;
            Update();
        }

        void Update() => Observers.Set(new ExpressionData(type, scope, new VariableExpressionData()));


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