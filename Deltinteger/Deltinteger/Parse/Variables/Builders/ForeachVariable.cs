using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse
{
    class ForeachVariable : VarBuilder
    {
        public ForeachVariable(Scope operationalScope, IVarContextHandler contextHandler) : base(operationalScope, contextHandler) {}

        protected override void CheckComponents()
        {
            RejectAttributes(
                new RejectAttributeComponent(
                    AttributeType.Public, AttributeType.Protected, AttributeType.Private,
                    AttributeType.Static,
                    AttributeType.GlobalVar, AttributeType.PlayerVar,
                    AttributeType.Ref
                ),
                new RejectComponent<WorkshopIndexComponent>(),
                new RejectComponent<ExtendedCollectionComponent>(),
                new RejectComponent<InitialValueComponent>()
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