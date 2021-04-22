using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    class RuleLevelVariable : VarBuilder
    {
        private readonly Scope _operationalScope;

        public RuleLevelVariable(Scope operationalScope, IVarContextHandler contextHandler) : base(contextHandler)
        {
            _operationalScope = operationalScope;
        }

        protected override void MissingAttribute(AttributeType[] missing)
        {
            // Syntax error if both the globalvar and playervar attributes are missing.
            if (missing.Contains(AttributeType.Globalvar) && missing.Contains(AttributeType.Playervar))
                _diagnostics.Error("Expected the globalvar/playervar attribute.", _nameRange);
        }

        protected override void CheckAttributes()
        {
            RejectAttributes(AttributeType.Ref, AttributeType.Static);
        }

        protected override void Apply()
        {
            _varInfo.WholeContext = true;
            _varInfo.AccessLevel = AccessLevel.Public; // Set the access level.
            _varInfo.InitialValueResolve = InitialValueResolve.ApplyBlock; // Get the inital value after elements have been resolved.
            _varInfo.CodeLensType = CodeLensSourceType.RuleVariable; // Set the code lens type.
            _varInfo.HandleRestrictedCalls = true; // Handle restricted calls.
        }

        protected override Scope OperationalScope() => _operationalScope;
    }

    class ScopedVariable : VarBuilder
    {
        private readonly Scope _operationalScope;

        public ScopedVariable(Scope operationalScope, IVarContextHandler contextHandler) : base(contextHandler)
        {
            _operationalScope = operationalScope;
        }

        protected override void CheckAttributes()
        {
            RejectAttributes(
                AttributeType.Public, AttributeType.Protected, AttributeType.Private,
                AttributeType.Static,
                AttributeType.Globalvar, AttributeType.Playervar,
                AttributeType.ID
            );
        }

        protected override void Apply()
        {
            _varInfo.WholeContext = false;
            _varInfo.CodeLensType = CodeLensSourceType.ScopedVariable;
            _varInfo.RequiresCapture = true;

            if (_varInfo.IsWorkshopReference && _varInfo.InitialValueContext == null)
                _diagnostics.Error("Variables with the 'ref' attribute must have an initial value.", _nameRange);
        }

        protected override Scope OperationalScope() => _operationalScope;
    }

    class ClassVariable : VarBuilder
    {
        private readonly Scope _objectScope;
        private readonly Scope _staticScope;

        public ClassVariable(Scope objectScope, Scope staticScope, IVarContextHandler contextHandler) : base(contextHandler)
        {
            _objectScope = objectScope;
            _staticScope = staticScope;
        }

        protected override void CheckAttributes()
        {
            RejectAttributes(
                AttributeType.Globalvar, AttributeType.Playervar,
                AttributeType.ID, AttributeType.Ext,
                AttributeType.Ref
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

        protected override Scope OperationalScope() => _varInfo.Static ? _staticScope : _objectScope;
    }

    class ParameterVariable : VarBuilder
    {
        private readonly Scope _operationalScope;
        private readonly Lambda.IBridgeInvocable _bridgeInvocable;

        public ParameterVariable(Scope operationalScope, IVarContextHandler contextHandler, Lambda.IBridgeInvocable bridgeInvocable) : base(contextHandler)
        {
            _operationalScope = operationalScope;
            _bridgeInvocable = bridgeInvocable;
        }

        protected override void CheckAttributes()
        {
            RejectAttributes(
                AttributeType.Public, AttributeType.Protected, AttributeType.Private,
                AttributeType.Static,
                AttributeType.Globalvar, AttributeType.Playervar,
                AttributeType.ID
            );
        }

        protected override void Apply()
        {
            _varInfo.WholeContext = true; // Shouldn't matter.
            _varInfo.CodeLensType = CodeLensSourceType.ParameterVariable;
            _varInfo.TokenType = TokenType.Parameter;
            _varInfo.BridgeInvocable = _bridgeInvocable;
            _varInfo.RequiresCapture = true;
        }

        protected override void TypeCheck()
        {
            // Get the 'ref' attribute.
            VarBuilderAttribute refAttribute = _attributes.FirstOrDefault(attribute => attribute.Type == AttributeType.Ref);

            // If the type is constant and the variable has the ref parameter, show a warning.
            if (refAttribute != null && _varInfo.Type != null && _varInfo.Type.IsConstant())
                _diagnostics.Warning("Constant workshop types have the 'ref' attribute by default.", refAttribute.Range);
        }

        protected override Scope OperationalScope() => _operationalScope;
    }

    class SubroutineParameterVariable : ParameterVariable
    {
        public SubroutineParameterVariable(Scope operationalScope, IVarContextHandler contextHandler) : base(operationalScope, contextHandler, null)
        {
        }

        protected override void CheckAttributes()
        {
            base.CheckAttributes();
            RejectAttributes(AttributeType.Ref);
        }

        protected override void GetCodeType()
        {
            var context = _contextHandler.GetCodeType();
            CodeType type = CodeType.GetCodeTypeFromContext(_parseInfo, context);

            if (type != null && type.IsConstant())
                _diagnostics.Error($"Constant types cannot be used in subroutine parameters.", context.Range);

            _varInfo.Type = type;
        }
    }

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
                ApplyCodeType(_contextualLambdaType.Parameters[_parameter]);
        }
    }

    class ForeachVariable : VarBuilder
    {
        private readonly Scope _operationalScope;

        public ForeachVariable(Scope operationalScope, IVarContextHandler contextHandler) : base(contextHandler)
        {
            _operationalScope = operationalScope;
        }

        protected override void CheckAttributes()
        {
            RejectAttributes(
                AttributeType.Public, AttributeType.Protected, AttributeType.Private,
                AttributeType.Static,
                AttributeType.Globalvar, AttributeType.Playervar,
                AttributeType.ID,
                AttributeType.Ext,
                AttributeType.Initial,
                AttributeType.Ref
            );
        }

        protected override void Apply()
        {
            _varInfo.WholeContext = false;
            _varInfo.IsWorkshopReference = true;
            _varInfo.RequiresCapture = true;
            _varInfo.CodeLensType = CodeLensSourceType.ScopedVariable;

            _varInfo.TokenType = TokenType.Variable;
            _varInfo.TokenModifiers.Add(TokenModifier.Declaration);
            _varInfo.TokenModifiers.Add(TokenModifier.Readonly);
        }

        protected override Scope OperationalScope() => _operationalScope;
    }
}