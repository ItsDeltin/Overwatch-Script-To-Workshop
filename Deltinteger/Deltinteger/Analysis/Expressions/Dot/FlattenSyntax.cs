using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Expressions.Dot
{
    class FlattenSyntax
    {
        /// <summary>This is the last dot token when there is no right-hand operand.</summary>
        public Token TrailingSeperator { get; private set; }
        /// <summary>The list of dot expressions.</summary>
        public IReadOnlyList<IParseExpression> Parts => _parts;
        /// <summary>The number of expressions.</summary>
        public int Count => _parts.Count;

        readonly List<IParseExpression> _parts = new List<IParseExpression>();

        public FlattenSyntax(BinaryOperatorExpression binaryOperatorExpression)
        {
            Flatten(binaryOperatorExpression);
        }

        void Flatten(BinaryOperatorExpression current)
        {
            // If the expression is a Tree, recursively flatten.
            if (current.Left is BinaryOperatorExpression lop && lop.IsDotExpression())
                Flatten(lop);
            // Otherwise, add the expression to the list.
            else
                _parts.Add(current.Left);

            // Get the expression to the right of the dot.

            // If the expression is a Tree, recursively flatten.
            if (current.Right is BinaryOperatorExpression rop && rop.IsDotExpression())
                Flatten(rop);
            // Otherwise, add the expression to the list.
            else
            {
                if (current.Right is MissingElement)
                    TrailingSeperator = current.Operator.Token;
                else
                    _parts.Add(current.Right);
            }
        }
    }
}