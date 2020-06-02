using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class SetVariableAction : IStatement
    {
        private VariableResolve VariableResolve { get; }
        private string Operation { get; }
        private IExpression Value { get; }
        private string Comment;

        public SetVariableAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.VarsetContext varsetContext)
        {
            IExpression variableExpression = parseInfo.GetExpression(scope, varsetContext.var);

            // Get the variable being set.
            VariableResolve = new VariableResolve(new VariableResolveOptions(), variableExpression, DocRange.GetRange(varsetContext), parseInfo.Script.Diagnostics);
            
            // Get the operation.
            if (varsetContext.statement_operation() != null)
            {
                Operation = varsetContext.statement_operation().GetText();

                // If there is no value, syntax error.
                if (varsetContext.val == null)
                    parseInfo.Script.Diagnostics.Error("Expected an expression.", DocRange.GetRange(varsetContext).end.ToRange());
                
                // Parse the value.
                else
                    Value = parseInfo.GetExpression(scope, varsetContext.val);
            }
            else if (varsetContext.INCREMENT() != null) Operation = "++";
            else if (varsetContext.DECREMENT() != null) Operation = "--";
        }

        public void Translate(ActionSet actionSet)
        {
            VariableElements elements = VariableResolve.ParseElements(actionSet);

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

            // The actions used to set the variable.
            Element[] actions;

            // Set Variable actions
            if (modifyOperation == null)
                actions = elements.IndexReference.SetVariable(value, elements.Target, elements.Index);
            // Modify Variable actions
            else
                actions = elements.IndexReference.ModifyVariable((Elements.Operation)modifyOperation, value, elements.Target, elements.Index);
            
            // Add the actions to the action set.
            actionSet.AddAction(actions);

            // Set action comments
            if (Comment != null)
            {
                // If there is just one action used to set or modify the variable, set that action's comment.
                if (actions.Length == 1)
                    actions[0].Comment = Comment;
                // If multiple actions are required, precede the comment with (#) where # is the order of the relevent action.
                else
                    for (int i = 0; i < actions.Length; i++)
                        actions[i].Comment = "(" + i + ") " + Comment;
            }
        }

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment)
        {
            Comment = comment;
        }
    }
}