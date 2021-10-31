using DS.Analysis.Variables;
using DS.Analysis.Expressions;

namespace DS.Analysis.Scopes
{
    class ScopedVariable : ScopedElement
    {
        readonly Variable variable;

        public ScopedVariable(string alias, Variable variable) : base(alias)
        {
            this.variable = variable;
        }


        public override IIdentifierHandler GetIdentifierHandler() => variable;
    }
}