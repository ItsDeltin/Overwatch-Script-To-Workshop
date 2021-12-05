using System;
using System.Reactive.Disposables;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Statements
{
    using Diagnostics;
    using Expressions;
    using Types.Semantics;

    class AssignmentStatement : Statement
    {
        readonly Expression value;

        public AssignmentStatement(ContextInfo context, Assignment syntax)
        {
            // Get the variable being assigned to and the value.
            Expression variable = AddDisposable(context.GetExpression(syntax.VariableExpression));
            value = AddDisposable(context.GetExpression(syntax.Value));

            SerialDisposable variableStatus = AddDisposable(new SerialDisposable());

            // Extract the variable.
            AddDisposable(variable.Subscribe(variableExpressionData =>
            {
                // Not a variable.
                if (variableExpressionData.Variable == null)
                    variableStatus.Disposable = context.Diagnostics.Error(Messages.ExpectedVariable(), syntax.VariableExpression.Range);

                // Make sure the value type is assignable to the variable type.
                else if (value != null)
                    variableStatus.Disposable = TypeValidation.IsAssignableTo(context, context.Diagnostics.CreateToken(syntax.Value.Range), variable.Type, value.Type);

                else
                    variableStatus.Disposable = null;
            }));
        }
    }
}