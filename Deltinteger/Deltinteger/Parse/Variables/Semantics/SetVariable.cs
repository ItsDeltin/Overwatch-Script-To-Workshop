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
        private readonly VariableResolve _variableResolve;
        private readonly IExpression _value;
        private readonly IAssignmentOperation _operation;
        private string _comment;

        public SetVariableAction(ParseInfo parseInfo, Scope scope, Assignment assignmentContext)
        {
            // Get the variable expression.
            IExpression variableExpression = parseInfo.GetExpression(scope, assignmentContext.VariableExpression);

            // Extract the variable data.
            _variableResolve = new VariableResolve(parseInfo, new VariableResolveOptions() { ShouldBeSettable = true }, variableExpression, assignmentContext.VariableExpression.Range);

            // Get the value.
            _value = parseInfo.SetExpectType(_variableResolve.SetVariable?.Type()).GetExpression(scope, assignmentContext.Value);

            // Get the operation.
            Token assignmentToken = assignmentContext.AssignmentToken;
            CodeType variableType = variableExpression.Type(), valueType = _value.Type();
            AssignmentOperator op = AssignmentOperation.OperatorFromTokenType(assignmentToken.TokenType);
            _operation = variableType.Operations.GetOperation(op, valueType);

            // No operators exist for the variable and value pair.
            if (_operation == null)
            {
                // If the variable type is any, use default operation.
                if (assignmentToken.TokenType == TokenType.Equal && variableType.Operations.DefaultAssignment
                    && CodeTypeHelpers.IsCompatibleWithAny(variableType)
                    && CodeTypeHelpers.IsCompatibleWithAny(valueType))
                    _operation = new AssignmentOperation(op, parseInfo.Types.Any());
                // Otherwise, add an error.
                else
                    parseInfo.Script.Diagnostics.Error("Operator '" + assignmentToken.Text + "' cannot be applied to the types '" + variableType.GetNameOrAny() + "' and '" + valueType.GetNameOrAny() + "'.", assignmentToken.Range);
            }

            if (_operation != null)
            {
                _operation.Validate(parseInfo, assignmentContext.AssignmentToken.Range, _value);
            }
        }

        public void Translate(ActionSet actionSet) => _operation.Resolve(new AssignmentOperationInfo(_comment, actionSet, _variableResolve.ParseElements(actionSet), _value.Parse(actionSet)));

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment) => _comment = comment;

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
            _resolve = new VariableResolve(parseInfo, new VariableResolveOptions(), variableExpr, increment.VariableExpression.Range);
        }

        public void Translate(ActionSet actionSet)
        {
            VariableElements elements = _resolve.ParseElements(actionSet);

            // Increment
            if (!_decrement)
                elements.IndexReference.Modify(actionSet, Operation.Add, (Element)1, elements.Target);
            // Decrement
            else
                elements.IndexReference.Modify(actionSet, Operation.Subtract, (Element)1, elements.Target);
        }

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment)
        {
            _comment = comment;
        }
    }
}