using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Expressions.Dot
{
    interface IFlattenSyntax
    {
        Token TrailingSeperator { get; }
        IReadOnlyList<ITreeContextPart> Parts { get; }
    }

    class FlattenSyntax : IFlattenSyntax
    {
        /// <summary>This is the last dot token when there is no right-hand operand.</summary>
        public Token TrailingSeperator { get; private set; }
        public IReadOnlyList<ITreeContextPart> Parts => _parts;

        readonly List<ITreeContextPart> _parts = new List<ITreeContextPart>();

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
                _parts.Add(new ExpressionPart(current.Left));

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
                    _parts.Add(new ExpressionPart(current.Right));
            }
        }
    }
}