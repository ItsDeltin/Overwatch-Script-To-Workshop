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
                new RejectAttributeComponent(
                    AttributeType.Public, AttributeType.Protected, AttributeType.Private,
                    AttributeType.Static,
                    AttributeType.GlobalVar, AttributeType.PlayerVar,
                    AttributeType.Ref
                ),
                new RejectComponent<WorkshopIndexComponent>(),
                new RejectComponent<ExtendedCollectionComponent>(),
                new RejectComponent<MacroComponent>()
            );
        }

        protected override void Apply()
        {
            _varInfo.WholeContext = false;
            _varInfo.CodeLensType = CodeLensSourceType.ScopedVariable;
            _varInfo.AccessLevel = AccessLevel.Public;
            // todo _varInfo.RequiresCapture = true;
        }
    }
}