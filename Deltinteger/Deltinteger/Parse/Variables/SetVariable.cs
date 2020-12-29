using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class SetVariableAction : IStatement
    {
        private VariableResolve VariableResolve { get; }
        private Token Operation { get; }
        private IExpression Value { get; }
        private string Comment;

        public SetVariableAction(ParseInfo parseInfo, Scope scope, Assignment assignmentContext)
        {
            IExpression variableExpression = parseInfo.GetExpression(scope, assignmentContext.VariableExpression);

            // Get the variable being set.
            VariableResolve = new VariableResolve(new VariableResolveOptions(), variableExpression, assignmentContext.VariableExpression.Range, parseInfo.Script.Diagnostics);
            
            // Get the operation.
            Operation = assignmentContext.AssignmentToken;

            Value = parseInfo.GetExpression(scope, assignmentContext.Value);
        }

        public void Translate(ActionSet actionSet)
        {
            VariableElements elements = VariableResolve.ParseElements(actionSet);
            IWorkshopTree value = Value.Parse(actionSet);

            Elements.Operation? modifyOperation = null;
            switch (Operation.Text)
            {
                case "=": break;
                case "^=": modifyOperation = Elements.Operation.RaiseToPower; break;
                case "*=": modifyOperation = Elements.Operation.Multiply;     break;
                case "/=": modifyOperation = Elements.Operation.Divide;       break;
                case "%=": modifyOperation = Elements.Operation.Modulo;       break;
                case "+=": modifyOperation = Elements.Operation.Add;          break;
                case "-=": modifyOperation = Elements.Operation.Subtract;     break;
                default: throw new Exception($"Unknown operation {Operation}.");
            }

            // TODO: update comment
            // Set Variable actions
            if (modifyOperation == null)
                elements.IndexReference.Set(actionSet, value, elements.Target, elements.Index);
            // Modify Variable actions
            else
                elements.IndexReference.Modify(actionSet, (Elements.Operation)modifyOperation, value, elements.Target, elements.Index);
        }

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment)
        {
            Comment = comment;
        }

        public static Element[] CommentAll(string comment, Element[] actions)
        {
            if (comment != null)
            {
                // If there is just one action used to set or modify the variable, set that action's comment.
                if (actions.Length == 1)
                    actions[0].Comment = comment;
                // If multiple actions are required, precede the comment with (#) where # is the order of the relevent action.
                else
                    for (int i = 0; i < actions.Length; i++)
                        actions[i].Comment = "(" + i + ") " + comment;
            }
            return actions;
        }
    }

    public class IncrementAction : IStatement
    {
        private readonly VariableResolve _resolve;
        private readonly bool _decrement;
        private string _comment;

        public IncrementAction(ParseInfo parseInfo, Scope scope, Increment increment)
        {
            _decrement = increment.Decrement;

            // Get the variable.
            IExpression variableExpr = parseInfo.GetExpression(scope, increment.VariableExpression);
            _resolve = new VariableResolve(new VariableResolveOptions(), variableExpr, increment.VariableExpression.Range, parseInfo.Script.Diagnostics);
        }

        public void Translate(ActionSet actionSet)
        {
            VariableElements elements = _resolve.ParseElements(actionSet);

            // Increment
            if (!_decrement)
                elements.IndexReference.Modify(actionSet, Operation.Add, (Element)1, elements.Target, elements.Index);
            // Decrement
            else
                elements.IndexReference.Modify(actionSet, Operation.Subtract, (Element)1, elements.Target, elements.Index);
        }

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment)
        {
            _comment = comment;
        }
    }
}