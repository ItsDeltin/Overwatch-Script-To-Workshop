using System;
using System.Linq;
using DS.Analysis.Scopes;
using DS.Analysis.Scopes.Selector;
using DS.Analysis.Types;
using DS.Analysis.Types.Standard;
using DS.Analysis.Diagnostics;
using DS.Analysis.Utility;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Expressions.Identifiers
{
    class IdentifierExpression : Expression
    {
        readonly ScopeWatcher scopeWatcher;
        readonly Token token;
        readonly FileDiagnostics diagnostics;

        readonly SerializedDisposableCollection state = new SerializedDisposableCollection();

        readonly AutoPushExpressionData expressionData;

        public IdentifierExpression(ContextInfo contextInfo, Identifier identifier)
        {
            this.token = identifier.Token;
            this.diagnostics = contextInfo.File.Diagnostics;
            expressionData = new AutoPushExpressionData(Observers.Set);

            // Create scope watcher.
            AddDisposable(scopeWatcher = contextInfo.Scope.Watch());

            // Watch changes.
            scopeWatcher.Subscribe(FilterIdentifiers);
        }

        void FilterIdentifiers(ScopeSourceChange newIdentifiers)
        {
            state.Dispose();
            expressionData.AutoPush = false;

            var element = ChooseScopedElement(newIdentifiers.Elements);

            // Subscribe to the identifier's type.
            var typeDirector = element.IdentifierHandler?.GetTypeDirector();

            if (typeDirector != null)
                // Identifier has a type
                state.Add(typeDirector.Subscribe(expressionData.SetType));
            else
                // Type is unknown
                state.Add(StandardTypes.Unknown.Director.Subscribe(expressionData.SetType));

            // Subscribe to the identifier's scope.
            if (element.IdentifierHandler != null)
                state.Add(element.IdentifierHandler.GetScopeDirector().Subscribe(expressionData.SetScope));

            // Set method groups.
            expressionData.MethodGroup = element.MethodGroup;

            expressionData.AutoPush = true;
            expressionData.Push();
        }

        IdentifiedElement ChooseScopedElement(ScopedElement[] scopedElements)
        {
            var matchingName = scopedElements.Reverse().Where(e => e.Name == token);
            var match = matchingName.FirstOrDefault();

            if (match != null)
                return match.ElementSelector.GetIdentifiedElement(new RelatedElements(matchingName));

            state.Add(diagnostics.Error(Messages.IdentifierDoesNotExist(token), token));
            return IdentifiedElement.Unknown;
        }

        // Since _currentTypeSubscription may change, we do not want to use 'Node.AddDisposable' as normal.
        // Dispose of it manually.
        public override void Dispose()
        {
            base.Dispose();
            state.Dispose();
        }
    }
}