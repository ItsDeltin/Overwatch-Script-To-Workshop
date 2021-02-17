using System.Linq;
using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse
{
    class ParameterVariable : VarBuilder
    {
        protected readonly Scope _operationalScope;
        private readonly Lambda.IBridgeInvocable _bridgeInvocable;

        public ParameterVariable(Scope operationalScope, IVarContextHandler contextHandler, Lambda.IBridgeInvocable bridgeInvocable) : base(operationalScope, contextHandler)
        {
            _operationalScope = operationalScope;
            _bridgeInvocable = bridgeInvocable;
        }

        protected override void CheckComponents()
        {
            RejectAttributes(
                new RejectAttributeComponent(
                    AttributeType.Public, AttributeType.Protected, AttributeType.Private,
                    AttributeType.Static,
                    AttributeType.GlobalVar, AttributeType.PlayerVar
                ),
                new RejectComponent<WorkshopIndexComponent>()
            );
        }

        protected override void Apply()
        {
            _varInfo.WholeContext = true; // Shouldn't matter.
            _varInfo.CodeLensType = CodeLensSourceType.ParameterVariable;
            _varInfo.TokenType = SemanticTokenType.Parameter;
            _varInfo.BridgeInvocable = _bridgeInvocable;
            _varInfo.RequiresCapture = true;
        }

        protected override void TypeCheck()
        {
            // Get the 'ref' attribute.
            var refAttribute = _components.FirstOrDefault(attribute => attribute is AttributeComponent attributeComponent && attributeComponent.Attribute == AttributeType.Ref);

            // If the type is constant and the variable has the ref parameter, show a warning.
            if (refAttribute != null && _varInfo.Type != null && _varInfo.Type.IsConstant())
                _diagnostics.Warning("Constant workshop types have the 'ref' attribute by default.", refAttribute.Range);
        }
    }
}