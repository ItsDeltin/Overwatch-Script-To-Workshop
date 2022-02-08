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
        readonly NamedDiagnosticToken token;

        public IdentifierExpression(ContextInfo context, Identifier identifier) : base(context)
        {
            this.token = context.Diagnostics.CreateNamedToken(identifier.Token);
            DependOnScope();
        }

        public override void Update()
        {
            base.Update();

            var identified = ChooseScopedElement();

            // Get the type director.
            var typeDirector = identified.IdentifierHandler.TypeDirector;
            if (typeDirector != null)
            {
                DependOn(typeDirector, DependencyMode.RemoveOnUpdate);
                PhysicalType = typeDirector.Type;
            }
            else // Type is unknown
                PhysicalType = StandardTypes.Unknown.Instance;

            MethodGroup = identified.MethodGroup;
        }

        IdentifiedElement ChooseScopedElement()
        {
            var matchingName = ScopedElements.Reverse().Where(e => e.Name == token.Name);
            var match = matchingName.FirstOrDefault();

            if (match != null)
                return match.ElementSelector.GetIdentifiedElement(new RelatedElements(matchingName));

            AddDisposable(token.Error(name => Messages.IdentifierDoesNotExist(name)), true);
            return IdentifiedElement.Unknown;
        }
    }
}