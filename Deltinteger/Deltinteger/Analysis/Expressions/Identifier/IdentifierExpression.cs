using System;
using System.Linq;
using DS.Analysis.Scopes.Selector;
using DS.Analysis.Types;
using DS.Analysis.Diagnostics;
using DS.Analysis.Core;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Expressions.Identifiers
{
    class IdentifierExpression
    {
        public IExpressionHost ExpressionHost => expressionHost;

        readonly ContextInfo context;
        readonly NamedDiagnosticToken token;
        readonly AutoExpressionHost expressionHost;

        public IdentifierExpression(ContextInfo context, Identifier identifier)
        {
            this.context = context;
            this.token = context.Diagnostics.CreateNamedToken(identifier.Token);
            expressionHost = context.CreateExpressionHost("identifier", Update);
            expressionHost.DependOn(context.Scope);
        }

        public void Update()
        {
            var identified = ChooseScopedElement();

            // Get the type director.
            var typeDirector = identified.IdentifierHandler.TypeDirector;
            if (typeDirector != null)
            {
                expressionHost.DependOnUntilUpdate(typeDirector);
                expressionHost.Type = typeDirector.Type;
            }
            else // Type is unknown
                expressionHost.Type = StandardType.Unknown.Instance;

            expressionHost.MethodGroup = identified.MethodGroup;
        }

        IdentifiedElement ChooseScopedElement()
        {
            if (token.GetName(out string name))
            {
                var matchingName = context.Scope.GetScopedElements().Reverse().Where(e => e.Name == name);
                var match = matchingName.FirstOrDefault();

                if (match != null)
                    return match.ElementSelector.GetIdentifiedElement(new RelatedElements(matchingName));

                expressionHost.DisposeOnUpdate(token.Error(Messages.IdentifierDoesNotExist(name)));
            }
            return IdentifiedElement.Unknown;
        }
    }
}