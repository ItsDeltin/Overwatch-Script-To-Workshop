using System;
using System.Reactive.Disposables;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Statements
{
    using Scopes;
    using Utility;
    using Diagnostics;
    using Expressions;
    using Types.Semantics;

    class AssignmentStatement : Statement
    {
        readonly Assignment syntax;
        readonly Expression variable;
        readonly Expression value;

        public AssignmentStatement(ContextInfo context, Assignment syntax) : base(context)
        {
            this.syntax = syntax;

            // Get the variable being assigned to and the value.
            variable = GetExpression(syntax.VariableExpression);
            value = GetExpression(syntax.Value);
            DependOnScope();
        }

        public override void Update()
        {
            base.Update();

            // Not a variable.
            if (variable.Variable == null)
                AddDisposable(Context.Diagnostics.Error(Messages.ExpectedVariable(), syntax.VariableExpression.Range), true);

            // Make sure the value type is assignable to the variable type.
            else if (value != null)
                AddDisposable(TypeValidation.IsAssignableTo(
                    context: Context,
                    token: Context.Diagnostics.CreateToken(syntax.Value.Range),
                    scopedElements: ScopedElements,
                    assignToType: variable.PhysicalType,
                    valueType: value.PhysicalType), true);
        }
    }
}