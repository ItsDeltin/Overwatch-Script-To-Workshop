using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse
{
    class ScopedVariable : VarBuilder
    {
        private readonly Scope _operationalScope;
        private readonly bool _canBeMacro;

        public ScopedVariable(bool canBeMacro, Scope operationalScope, IVarContextHandler contextHandler) : base(operationalScope, contextHandler)
        {
            _operationalScope = operationalScope;
            _canBeMacro = canBeMacro;
            _canInferType = true;
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

            if (_canBeMacro)
                RejectVirtualIfNotMacro();
            else
                RejectAttributes(new RejectComponent<MacroComponent>());
        }

        protected override void Apply()
        {
            _varInfo.WholeContext = false;
            _varInfo.CodeLensType = CodeLensSourceType.ScopedVariable;
            _varInfo.RequiresCapture = true;
        }
    }
}