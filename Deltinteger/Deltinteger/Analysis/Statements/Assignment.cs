using System;
using System.Reactive.Disposables;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Statements
{
    using Utility;
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

            var scopeWatcher = AddDisposable(context.Scope.Watch());
            AddDisposable(Helper.Observe(scopeWatcher, variable, value, (scopeElements, variableData, valueData) =>
            {
                // Not a variable.
                if (variableData.Variable == null)
                    return context.Diagnostics.Error(Messages.ExpectedVariable(), syntax.VariableExpression.Range);

                // Make sure the value type is assignable to the variable type.
                else if (value != null)
                    return TypeValidation.IsAssignableTo(context, context.Diagnostics.CreateToken(syntax.Value.Range), scopeElements, variableData.Type, valueData.Type);

                else
                    return Disposable.Empty;
            }));
        }
    }
}