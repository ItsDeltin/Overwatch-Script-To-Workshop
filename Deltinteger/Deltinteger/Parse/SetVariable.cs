using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class SetVariableAction : IStatement
    {
        private Var SetVariable { get; }
        private ExpressionTree Tree { get; }
        private IExpression Value { get; }

        public SetVariableAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.VarsetContext varsetContext)
        {
            IExpression variableExpression = DeltinScript.GetExpression(script, translateInfo, scope, varsetContext.var);

            DocRange notAVariableRange = null;

            if (variableExpression is Var)
            {
                SetVariable = (Var)variableExpression;
            }
            else if (variableExpression is ExpressionTree)
            {
                Tree = (ExpressionTree)variableExpression;
                if (Tree.Completed)
                {
                    if (Tree.Result is Var == false)
                        notAVariableRange = DocRange.GetRange(Tree.ExprContextTree.Last());
                    else
                        SetVariable = (Var)Tree.Result;
                }
            }
            else if (variableExpression != null)
                notAVariableRange = DocRange.GetRange(varsetContext.var);

            if (notAVariableRange != null)
                script.Diagnostics.Error("Expected a variable.", notAVariableRange);
            
            if (varsetContext.statement_operation() != null && varsetContext.val == null)
                script.Diagnostics.Error("Expected an expression.", DocRange.GetRange(varsetContext).end.ToRange());
            else
                Value = DeltinScript.GetExpression(script, translateInfo, scope, varsetContext.val);
        }

        public void Translate(ActionSet actionSet)
        {
            IGettable var;
            if (Tree != null)
                var = Tree.ParseTree(actionSet).ResultingVariable;
            else
                var = actionSet.IndexAssigner[SetVariable];

            // TODO: Don't cast to Element.
            actionSet.AddAction(((IndexReference)var).SetVariable(
                (Element)Value.Parse(actionSet)
            ));
        }
    }
}