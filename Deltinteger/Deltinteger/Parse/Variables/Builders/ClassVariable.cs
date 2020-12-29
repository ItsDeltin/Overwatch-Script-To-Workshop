using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse
{
    class ClassVariable : VarBuilder
    {
        public ClassVariable(IScopeHandler scopeHandler, IVarContextHandler contextHandler) : base(scopeHandler, contextHandler) {}

        protected override void CheckComponents()
        {
            RejectAttributes(
                new RejectAttributeComponent(
                    AttributeType.GlobalVar, AttributeType.PlayerVar,
                    AttributeType.Ref
                ),
                new RejectComponent<WorkshopIndexComponent>(),
                new RejectComponent<ExtendedCollectionComponent>()
            );
            RejectVirtualIfNotMacro();
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