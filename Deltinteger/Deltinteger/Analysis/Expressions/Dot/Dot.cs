using System;
using System.Linq;
using DS.Analysis.Types;
using DS.Analysis.Scopes;
using DS.Analysis.Utility;
using DS.Analysis.Core;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Expressions.Dot
{
    class DotExpression : Expression
    {
        // Operands in a dot tree (x.(y.z)) flattened (x.y.z)
        readonly FlattenSyntax flattenSyntax;
        readonly DotNode[] nodes;

        public DotExpression(ContextInfo context, BinaryOperatorExpression binaryOperatorSyntax) : base(context)
        {
            flattenSyntax = new FlattenSyntax(binaryOperatorSyntax);

            // Create the expression nodes array.
            nodes = new DotNode[flattenSyntax.Count];

            // Assign the values in the nodes array.
            for (int i = 0; i < nodes.Length; i++)
                nodes[i] = AddDisposable(new DotNode(
                    context: Context,
                    syntax: flattenSyntax.Parts[i],
                    parent: i == 0 ? null : nodes[i - 1],
                    position: i == 0 ? NodePosition.First :
                              i == nodes.Length - 1 ? NodePosition.Last : NodePosition.Middle
                ));

            // Depend on the final node.
            DependOn(nodes.Last().Expression);
        }

        public override void Update()
        {
            base.Update();
            CopyStateOf(nodes.Last().Expression);
        }

        class DotNode : IDisposable
        {
            public IExpressionHost Expression { get; }

            readonly SerialScopeSource serialScope;

            readonly SingleNode node;

            public DotNode(ContextInfo context, IParseExpression syntax, DotNode parent, NodePosition position)
            {
                node = context.Analysis.SingleNode(() =>
                {
                    // Update the serialScope.
                    if (position != NodePosition.First)
                        serialScope.Elements = Expression.ScopeSource.Elements;
                });

                // Get the expression.
                ContextInfo partContext = context;

                // If this is not the first expression, clear tail data and set the source expression.
                if (position != NodePosition.First)
                    partContext = partContext.ClearTail().SetSourceExpression(parent.Expression).SetScope(parent.serialScope);
                // If this is not the last expression, clear head data.
                if (position != NodePosition.Last)
                    partContext = partContext.ClearHead();

                // Get the expression.
                Expression = partContext.GetExpression(syntax);

                node.AddDisposable(Expression);

                // Create the scope that the next DotNode will use.
                node.DependOn(Expression.ScopeSource);

                // Create the serialScope.
                serialScope = new SerialScopeSource(Expression.ScopeSource.Elements);
            }

            public void Dispose()
            {
                node.Dispose();
            }
        }

        enum NodePosition
        {
            First,
            Middle,
            Last
        }
    }
}