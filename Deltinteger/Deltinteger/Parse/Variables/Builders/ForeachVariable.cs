using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse
{
    class ForeachVariable : VarBuilder
    {
        public ForeachVariable(Scope operationalScope, IVarContextHandler contextHandler) : base(operationalScope, contextHandler) {}

        protected override void CheckComponents()
        {
            RejectAttributes(
                new AttributeComponentIdentifier(
                    AttributeType.Public, AttributeType.Protected, AttributeType.Private,
                    AttributeType.Static,
                    AttributeType.GlobalVar, AttributeType.PlayerVar,
                    AttributeType.Ref, AttributeType.In
                ),
                new ComponentIdentifier<WorkshopIndexComponent>(),
                new ComponentIdentifier<ExtendedCollectionComponent>(),
                new ComponentIdentifier<InitialValueComponent>()
            );
        }

        protected override void Apply()
        {
            _varInfo.WholeContext = false;
            _varInfo.VariableTypeHandler.SetWorkshopReference();
            _varInfo.RequiresCapture = true;
            _varInfo.CodeLensType = CodeLensSourceType.ScopedVariable;

            _varInfo.TokenType = SemanticTokenType.Variable;
            _varInfo.TokenModifiers.Add(TokenModifier.Declaration);
            _varInfo.TokenModifiers.Add(TokenModifier.Readonly);
        }
    }
}