namespace Deltin.Deltinteger.Parse.Variables.Build
{
    class StructVariableBuilder : VarBuilder
    {
        public StructVariableBuilder(IScopeHandler scopeHandler, IVarContextHandler contextHandler) : base(scopeHandler, contextHandler) { }

        protected override void CheckComponents()
        {
            RejectAttributes(
                new AttributeComponentIdentifier(
                    AttributeType.GlobalVar, AttributeType.PlayerVar, AttributeType.Persist,
                    AttributeType.Ref, AttributeType.In,
                    AttributeType.Virtual, AttributeType.Override
                ),
                new ComponentIdentifier<WorkshopIndexComponent>(),
                new ComponentIdentifier<ExtendedCollectionComponent>()
            );
        }

        protected override void Apply()
        {
            if (!_varInfo.Static && _varInfo.Type != null && _varInfo.Type.IsConstant())
                _diagnostics.Error("Non-static variables with workshop constant types are not allowed.", _typeRange);

            _varInfo.WholeContext = true;
            _varInfo.CodeLensType = CodeLensSourceType.ClassVariable;
            _varInfo.InitialValueResolve = InitialValueResolve.ApplyBlock;
        }
    }
}