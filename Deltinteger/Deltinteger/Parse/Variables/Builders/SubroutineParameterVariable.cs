using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse
{
    class SubroutineParameterVariable : ParameterVariable
    {
        public SubroutineParameterVariable(Scope operationalScope, IVarContextHandler contextHandler) : base(operationalScope, contextHandler, null) {}

        protected override void CheckComponents()
        {
            base.CheckComponents();
            RejectAttributes(new RejectAttributeComponent(AttributeType.Ref, AttributeType.In));
        }

        protected override void ApplyCodeType(CodeType type)
        {            
            if (type != null && type.IsConstant())
                _diagnostics.Error($"Constant types cannot be used in subroutine parameters.", _typeRange);
            
            _varInfo.Type = type;
        }
    }
}