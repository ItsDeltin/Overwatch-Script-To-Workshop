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
            _varInfo.OperationalScope = _operationalScope; // Set the operational scope.
            _varInfo.AccessLevel = AccessLevel.Public; // Set the access level.
            _varInfo.InitialValueResolve = InitialValueResolve.ApplyBlock; // Get the inital value after elements have been resolved.
            _varInfo.CodeLensType = CodeLensSourceType.RuleVariable; // Set the code lens type.
        }
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
                AttributeType.Ref,
                AttributeType.Static,
                AttributeType.Globalvar, AttributeType.Playervar,
                AttributeType.ID
            );
        }

        protected override void Apply()
        {
            _varInfo.WholeContext = false;
            _varInfo.OperationalScope = _operationalScope;
            _varInfo.CodeLensType = CodeLensSourceType.ScopedVariable;
        }
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
            _varInfo.OperationalScope = _varInfo.Static ? _staticScope : _objectScope;
            _varInfo.CodeLensType = CodeLensSourceType.ClassVariable;
        }
    }

    class ParameterVariable : VarBuilder
    {
        private readonly Scope _operationalScope;

        public ParameterVariable(Scope operationalScope, IVarContextHandler contextHandler) : base(contextHandler)
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
            _varInfo.WholeContext = true; // Shouldn't matter.
            _varInfo.OperationalScope = _operationalScope;
            _varInfo.CodeLensType = CodeLensSourceType.ParameterVariable;
        }

        protected override void TypeCheck()
        {
            // Get the 'ref' attribute.
            VarBuilderAttribute refAttribute = _attributes.FirstOrDefault(attribute => attribute.Type == AttributeType.Ref);

            // If the type is constant and the variable has the ref parameter, show a warning.
            if (refAttribute != null && _varInfo.Type != null && _varInfo.Type.IsConstant())
                _diagnostics.Warning("Constant workshop types have the 'ref' attribute by default.", refAttribute.Range);
        }
    }

    class SubroutineParameterVariable : ParameterVariable
    {
        public SubroutineParameterVariable(Scope operationalScope, IVarContextHandler contextHandler) : base(operationalScope, contextHandler)
        {
        }

        protected override void CheckAttributes()
        {
            base.CheckAttributes();
            RejectAttributes(AttributeType.Ext, AttributeType.Ref);
        }

        protected override void GetCodeType()
        {
            var context = _contextHandler.GetCodeType();
            CodeType type = CodeType.GetCodeTypeFromContext(_parseInfo, context);
            
            if (type != null && type.IsConstant())
                _diagnostics.Error($"Constant types cannot be used in subroutine parameters.", DocRange.GetRange(context));
            
            _varInfo.Type = type;
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
            _varInfo.OperationalScope = _operationalScope;
            _varInfo.CodeLensType = CodeLensSourceType.ScopedVariable;
        }
    }

    class AutoForVariable : VarBuilder
    {
        private readonly Scope _operationalScope;

        public AutoForVariable(Scope operationalScope, IVarContextHandler contextHandler) : base(contextHandler)
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
                AttributeType.Ref
            );
        }

        protected override void Apply()
        {
            _varInfo.WholeContext = false;
            _varInfo.OperationalScope = _operationalScope;
            _varInfo.CodeLensType = CodeLensSourceType.ScopedVariable;
        }
    }
}