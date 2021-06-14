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

        public VariableResolve(ParseInfo parseInfo, VariableResolveOptions options, IExpression expression, DocRange expressionRange)
            : this(parseInfo, options, expression, expressionRange, new VariableResolveErrorHandler(parseInfo.Script.Diagnostics))
        {}

        public VariableResolve(ParseInfo parseInfo, VariableResolveOptions options, IExpression expression, DocRange expressionRange, IVariableResolveErrorHandler errorHandler)
        {
            bool treeSettable = true;

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
                        treeSettable = tree.TargetCanBeSet();
                    }
                }
            }
            // The expression is not a variable.
            else if (expression != null)
                NotAVariableRange = expressionRange;

            // NotAVariableRange will not be null if the resulting expression is a variable.
            if (NotAVariableRange != null)
                errorHandler.Error("Expected a variable.", NotAVariableRange);

            // Make sure the variable can be set to.
            if (SetVariable != null)
            {
                // Check if the variable is settable.
                if (options.ShouldBeSettable)
                {
                    // The variable can never be set.
                    if (!SetVariable.Calling.Attributes.CanBeSet)
                        errorHandler.Error($"The variable '{SetVariable.Calling.Name}' cannot be set", VariableRange);

                    // The variable is normally settable, but not in the current context.
                    else if (!treeSettable || (parseInfo.ContextualVariableModifiers != null && !parseInfo.ContextualVariableModifiers.IsSettable(SetVariable.Calling)))
                        errorHandler.Error($"The variable '{SetVariable.Calling.Name}' cannot be set in the current context", VariableRange);
                }
                
                // Check if the variable is a whole workshop variable.
                else if ((options.FullVariable && SetVariable.Calling.Attributes.StoreType != StoreType.FullVariable) || (!options.CanBeIndexed && SetVariable.Index.Length != 0))
                    errorHandler.Error($"The variable '{SetVariable.Calling.Name}' cannot be indexed", VariableRange);
            }

            DoesResolveToVariable = SetVariable != null;
        }

        public VariableElements ParseElements(ActionSet actionSet)
        {
            IGettable var;
            Element target = null;
            Element[] index;

            if (Tree != null)
            {
                // Parse the tree.
                ExpressionTreeParseResult treeParseResult = Tree.ParseTree(actionSet, true);
                // Get the variable.
                var = treeParseResult.ResultingVariable;
                // Get the target.
                target = treeParseResult.Target as Element;
                // Get the index.
                index = treeParseResult.ResultingIndex;
            }
            else
            {
                // Get the variable.
                var = actionSet.IndexAssigner[SetVariable.Calling.Provider];
                // Get the index.
                index = Array.ConvertAll(SetVariable.Index, index => (Element)index.Parse(actionSet));
            }

            return new VariableElements(var, target, index);
        }
    }
}