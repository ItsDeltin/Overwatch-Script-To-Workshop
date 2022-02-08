using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Expressions
{
    class BooleanAction : Expression
    {
        public bool Value { get; }

        public BooleanAction(ContextInfo context, BooleanExpression syntax) : base(context)
        {
            Value = syntax.Value;
        }
    }
}