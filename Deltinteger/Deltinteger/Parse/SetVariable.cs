using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class SetVariableAction : IStatement
    {
        public Var SetVariable { get; }
        public IExpression Value { get; }

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
                var tree = (ExpressionTree)variableExpression;
                if (tree.Completed)
                {
                    if (tree.Result is Var == false)
                        notAVariableRange = DocRange.GetRange(tree.ExprContextTree.Last());
                    else
                    {
                        SetVariable = (Var)tree.Result;
                    }
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
            // TODO: Don't cast to Element.
            actionSet.AddAction(
                ((IndexReference)actionSet.IndexAssigner[SetVariable]).SetVariable(
                    (Element)Value.Parse(actionSet)
                )
            );
        }
    }
}