using System;
using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Parse
{
    class RuleLevelVariable : VarBuilder
    {
        public RuleLevelVariable(IVarContextHandler contextHandler) : base(contextHandler) {}

        protected override void MissingAttribute(AttributeType[] attributeTypes)
        {
            // Syntax error if both the globalvar and playervar attributes are missing.
            if (!attributeTypes.Contains(AttributeType.Globalvar) && !attributeTypes.Contains(AttributeType.Playervar))
                _diagnostics.Error("Expected the globalvar/playervar attribute.", _nameRange);
        }

        protected override void CheckAttributes()
        {
            RejectAttributes(AttributeType.Ref, AttributeType.Static);
        }
    }

    class ScopedVariable : VarBuilder
    {
        public ScopedVariable(IVarContextHandler contextHandler) : base(contextHandler) {}

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
    }

    class ClassVariable : VarBuilder
    {
        public ClassVariable(IVarContextHandler contextHandler) : base(contextHandler) {}

        protected override void CheckAttributes()
        {
            RejectAttributes(
                AttributeType.Globalvar, AttributeType.Playervar,
                AttributeType.ID, AttributeType.Ext,
                AttributeType.Ref
            );
        }
    }

    class ParameterVariable : VarBuilder
    {
        public ParameterVariable(IVarContextHandler contextHandler) : base(contextHandler) {}

        protected override void CheckAttributes()
        {
            RejectAttributes(
                AttributeType.Public, AttributeType.Protected, AttributeType.Private,
                AttributeType.Static,
                AttributeType.Globalvar, AttributeType.Playervar,
                AttributeType.ID
            );
        }
    }

    class ForeachVariable : VarBuilder
    {
        public ForeachVariable(IVarContextHandler contextHandler) : base(contextHandler) {}

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
    }

    class AutoForVariable : VarBuilder
    {
        public AutoForVariable(IVarContextHandler contextHandler) : base(contextHandler) {}

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
    }
}