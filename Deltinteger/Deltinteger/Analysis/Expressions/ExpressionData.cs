using DS.Analysis.Types;
using DS.Analysis.Scopes;

namespace DS.Analysis.Expressions
{
    class ExpressionData
    {
        public CodeType Type { get; }
        public Scope Scope { get; }
        public VariableExpressionData Variable { get; }


        public ExpressionData(CodeType type)
        {
            Type = type;
            Scope = new Scope(type.Content.ScopeSource);
        }

        public ExpressionData(CodeType type, Scope scope, VariableExpressionData variable)
        {
            Type = type;
            Scope = scope;
            Variable = variable;
        }
    }
}