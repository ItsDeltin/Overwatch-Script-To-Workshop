using System;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class VariableResolve
    {
        public bool DoesResolveToVariable { get; }

        private DocRange NotAVariableRange { get; }
        private DocRange VariableRange { get; }

        public CallVariableAction SetVariable { get; }
        private ExpressionTree Tree { get; }

        public VariableResolve(VariableResolveOptions options, IExpression expression, DocRange expressionRange, FileDiagnostics diagnostics)
        {
            // The expression is a variable.
            if (expression is CallVariableAction)
            {
                // Get the variable being set and the range.
                SetVariable = (CallVariableAction)expression;
                VariableRange = expressionRange;
            }
            // The expression is an expression tree.
            else if (expression is ExpressionTree tree)
            {
                Tree = tree;
                if (tree.Completed)
                {
                    // If the resulting expression in the tree is not a variable.
                    if (tree.Result is CallVariableAction == false)
                        NotAVariableRange = tree.ExprContextTree.Last().GetRange();
                    else
                    {
                        // Get the variable and the range.
                        SetVariable = (CallVariableAction)tree.Result;
                        VariableRange = tree.ExprContextTree.Last().GetRange();
                    }
                }
            }
            // The expression is not a variable.
            else if (expression != null)
                NotAVariableRange = expressionRange;

            // NotAVariableRange will not be null if the resulting expression is a variable.
            if (NotAVariableRange != null)
                diagnostics.Error("Expected a variable.", NotAVariableRange);

            // Make sure the variable can be set to.
            if (SetVariable != null)
            {
                // Check if the variable is settable.
                if (options.ShouldBeSettable && !SetVariable.Calling.Settable())
                    diagnostics.Error($"The variable '{SetVariable.Calling.Name}' cannot be set to.", VariableRange);

                // Check if the variable is a whole workshop variable.
                if (options.FullVariable)
                {
                    Var asVar = SetVariable.Calling as Var;
                    if (asVar == null || asVar.StoreType != StoreType.FullVariable)
                        diagnostics.Error($"The variable '{SetVariable.Calling.Name}' cannot be indexed.", VariableRange);
                }

                // Check for indexers.
                if (!options.CanBeIndexed && SetVariable.Index.Length != 0)
                    diagnostics.Error($"The variable '{SetVariable.Calling.Name}' cannot be indexed.", VariableRange);
            }

            DoesResolveToVariable = SetVariable != null;
        }

        public VariableElements ParseElements(ActionSet actionSet)
        {
            IndexReference var;
            Element target = null;
            Element[] index;

            if (Tree != null)
            {
                // Parse the tree.
                ExpressionTreeParseResult treeParseResult = Tree.ParseTree(actionSet, true);
                // Get the variable.
                var = (IndexReference)treeParseResult.ResultingVariable;
                // Get the target.
                target = (Element)treeParseResult.Target;
                // Get the index.
                index = treeParseResult.ResultingIndex;
            }
            else
            {
                // Get the variable.
                var = (IndexReference)actionSet.IndexAssigner[SetVariable.Calling];
                // Get the index.
                index = Array.ConvertAll(SetVariable.Index, index => (Element)index.Parse(actionSet));
            }

            return new VariableElements(var, target, index);
        }
    }

    public class VariableElements
    {
        public IndexReference IndexReference { get; }
        public Element Target { get; }
        public Element[] Index { get; }

        public VariableElements(IndexReference indexReference, Element target, Element[] index)
        {
            IndexReference = indexReference;
            Target = target;
            Index = index;
        }
    }

    public class VariableResolveOptions
    {
        /// <summary>Determines if a variables needs to be an entire workshop variable.</summary>
        public bool FullVariable = false;
        /// <summary>Determines if the variable can be set to a value in an array.</summary>
        public bool CanBeIndexed = true;
        /// <summary>Determines if the variable should be settable.</summary>
        public bool ShouldBeSettable = true;
    }
}