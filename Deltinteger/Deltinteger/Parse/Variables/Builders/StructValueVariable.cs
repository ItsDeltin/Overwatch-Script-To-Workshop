namespace Deltin.Deltinteger.Parse.Variables.Build
{
    class StructValueVariable : VarBuilder
    {
        public StructValueVariable(IScopeHandler scopeHandler, IVarContextHandler contextHandler) : base(scopeHandler, contextHandler)
        {
            _canInferType = true;
        }

        protected override void CheckComponents()
        {
            RejectAttributes(
                new AttributeComponentIdentifier(
                    AttributeType.Public, AttributeType.Protected, AttributeType.Private,
                    AttributeType.Static,
                    AttributeType.GlobalVar, AttributeType.PlayerVar, AttributeType.Persist,
                    AttributeType.Ref, AttributeType.In
                ),
                new ComponentIdentifier<WorkshopIndexComponent>(),
                new ComponentIdentifier<ExtendedCollectionComponent>(),
                new ComponentIdentifier<MacroComponent>()
            );
        }

        protected override void Apply()
        {
            _varInfo.WholeContext = false;
            _varInfo.CodeLensType = CodeLensSourceType.ScopedVariable;
            _varInfo.AccessLevel = AccessLevel.Public;
            _varInfo.TokenType = SemanticTokenType.Parameter;
            // todo _varInfo.RequiresCapture = true;
        }
    }
}