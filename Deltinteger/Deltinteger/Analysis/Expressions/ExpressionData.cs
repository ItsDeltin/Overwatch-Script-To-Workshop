using DS.Analysis.Types;
using DS.Analysis.Scopes;

namespace DS.Analysis.Expressions
{
    class ExpressionData
    {
        public CodeType Type { get; }
        public Scope Scope { get; }


        public ExpressionData(CodeType type, Scope scope)
        {
            Type = type;
            Scope = scope;
        }
    }
}