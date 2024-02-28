using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse
{
    class SubroutineParameterVariable : ParameterVariable
    {
        readonly bool variableIsGlobal;

        public SubroutineParameterVariable(Scope operationalScope, IVarContextHandler contextHandler, bool variableIsGlobal) : base(operationalScope, contextHandler, null)
        {
            this.variableIsGlobal = variableIsGlobal;
        }

        protected override void CheckComponents()
        {
            base.CheckComponents();
            RejectAttributes(new AttributeComponentIdentifier(AttributeType.Ref, AttributeType.In, AttributeType.Persist));
        }

        protected override void ApplyCodeType(CodeType type)
        {
            if (type != null && type.IsConstant())
                _diagnostics.Error($"Constant types cannot be used in subroutine parameters.", _typeRange);

            _varInfo.Type = type;
        }

        protected override VariableSetKind GetVariableSetKind() =>
            variableIsGlobal ? VariableSetKind.Global : VariableSetKind.Player;
    }
}