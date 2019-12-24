using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class SetVariableAction : IStatement
    {
        private CallVariableAction SetVariable { get; }
        private ExpressionTree Tree { get; }
        private string Operation { get; }
        private IExpression Value { get; }

        public SetVariableAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.VarsetContext varsetContext)
        {
            IExpression variableExpression = DeltinScript.GetExpression(script, translateInfo, scope, varsetContext.var);

            DocRange notAVariableRange = null;
            DocRange variableRange = null;

            if (variableExpression is CallVariableAction)
            {
                SetVariable = (CallVariableAction)variableExpression;
                variableRange = DocRange.GetRange(varsetContext.var);
            }
            else if (variableExpression is ExpressionTree)
            {
                Tree = (ExpressionTree)variableExpression;
                if (Tree.Completed)
                {
                    if (Tree.Result is CallVariableAction == false)
                        notAVariableRange = Tree.ExprContextTree.Last().Range;
                    else
                    {   
                        SetVariable = (CallVariableAction)Tree.Result;
                        variableRange = Tree.ExprContextTree.Last().Range;
                    }
                }
            }
            else if (variableExpression != null)
                notAVariableRange = DocRange.GetRange(varsetContext.var);

            if (notAVariableRange != null)
                script.Diagnostics.Error("Expected a variable.", notAVariableRange);
            
            if (SetVariable != null && !SetVariable.Calling.Settable())
                script.Diagnostics.Error($"The variable '{SetVariable.Calling.Name}' cannot be set to.", variableRange);
            
            if (varsetContext.statement_operation() != null)
            {
                Operation = varsetContext.statement_operation().GetText();
                if (varsetContext.val == null)
                    script.Diagnostics.Error("Expected an expression.", DocRange.GetRange(varsetContext).end.ToRange());
                else
                    Value = DeltinScript.GetExpression(script, translateInfo, scope, varsetContext.val);
            }
            else if (varsetContext.INCREMENT() != null) Operation = "++";
            else if (varsetContext.DECREMENT() != null) Operation = "--";
        }

        public void Translate(ActionSet actionSet)
        {
            IGettable var;
            Element target = null;
            Element[] index;
            if (Tree != null)
            {
                ExpressionTreeParseResult treeParseResult = Tree.ParseTree(actionSet, true, true);
                var = treeParseResult.ResultingVariable;
                target = (Element)treeParseResult.Target;
                index = treeParseResult.ResultingIndex;
            }
            else
            {
                var = actionSet.IndexAssigner[SetVariable.Calling];
                index = Array.ConvertAll(SetVariable.Index, index => (Element)index.Parse(actionSet));
            }

            Element value = null;
            if (Value != null) value = (Element)Value.Parse(actionSet);

            Elements.Operation? modifyOperation = null;
            switch (Operation)
            {
                case "=": break;
                case "^=": modifyOperation = Elements.Operation.RaiseToPower; break;
                case "*=": modifyOperation = Elements.Operation.Multiply;     break;
                case "/=": modifyOperation = Elements.Operation.Divide;       break;
                case "%=": modifyOperation = Elements.Operation.Modulo;       break;
                case "+=": modifyOperation = Elements.Operation.Add;          break;
                case "-=": modifyOperation = Elements.Operation.Subtract;     break;
                case "++": value = 1; modifyOperation = Elements.Operation.Add;      break;
                case "--": value = 1; modifyOperation = Elements.Operation.Subtract; break;
                default: throw new Exception($"Unknown operation {Operation}.");
            }

            if (modifyOperation == null)
                actionSet.AddAction(((IndexReference)var).SetVariable(value, target, index));
            else
                actionSet.AddAction(((IndexReference)var).ModifyVariable((Elements.Operation)modifyOperation, value, target, index));

        }
    }
}