using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class SetVariableAction : CodeAction, IStatement
    {
        public Var SetVariable { get; }
        public IExpression Value { get; }

        public SetVariableAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.VarsetContext varsetContext)
        {
            IExpression variableExpression = GetExpression(script, translateInfo, scope, varsetContext.var);

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
            else notAVariableRange = DocRange.GetRange(varsetContext.var);

            if (notAVariableRange != null)
                script.Diagnostics.Error("Expected a variable.", notAVariableRange);
            
            if (varsetContext.statement_operation() != null && varsetContext.val == null)
                script.Diagnostics.Error("Expected an expression.", DocRange.GetRange(varsetContext).end.ToRange());
            else
                Value = GetExpression(script, translateInfo, scope, varsetContext.val);
        }
    }
}