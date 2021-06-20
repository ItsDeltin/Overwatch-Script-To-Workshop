namespace Deltin.Deltinteger.Parse
{
    class LambdaVariable : ParameterVariable
    {
        private readonly int _parameter;
        private readonly Lambda.PortableLambdaType _contextualLambdaType;

        public LambdaVariable(int parameter, Lambda.PortableLambdaType contextualLambdaType, Scope operationalScope, IVarContextHandler contextHandler, Lambda.IBridgeInvocable invocable)
            : base(operationalScope, contextHandler, invocable)
        {
            _parameter = parameter;
            _contextualLambdaType = contextualLambdaType;
        }

        protected override void GetCodeType()
        {
            // If the lambda type derived from the current context is null,
            // or the parameter types of the contextual lambda type is unknown,
            // or the contextual lambda type does not have a type for our parameter index,
            if (_contextualLambdaType == null || !_contextualLambdaType.ParameterTypesKnown || _parameter >= _contextualLambdaType.Parameters.Length)
                // then use the default GetCodeType implementation.
                base.GetCodeType();
            // Otherwise, we can supply the parameter code type from the contextual lambda type.
            else
            {
                var inferredType = _contextualLambdaType.Parameters[_parameter];

                // If an explicit type was provided, make sure the inferred type matches.
                if (_contextHandler.GetCodeType() != null)
                {
                    CodeType type = TypeFromContext.GetCodeTypeFromContext(_parseInfo, _operationalScope, _contextHandler.GetCodeType());

                    if (!type.Is(inferredType))
                        _parseInfo.Script.Diagnostics.Error("Expected the '" + inferredType.GetName() + "' type", _contextHandler.GetTypeRange());
                }

                ApplyCodeType(inferredType);
            }
        }
    }
}