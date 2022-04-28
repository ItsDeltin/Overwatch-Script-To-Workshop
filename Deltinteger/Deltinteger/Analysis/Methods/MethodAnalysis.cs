using System;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Methods
{
    using Expressions;
    using Core;

    class MethodAnalysis : IDisposable
    {
        readonly IExpressionHost target;
        readonly SingleNode dependencyHandler;

        public MethodAnalysis(ContextInfo context, FunctionExpression syntax)
        {
            target = context.GetExpression(syntax.Target);
            dependencyHandler = context.Analysis.SingleNode(() =>
            {
                // If the expression is a method group
                if (target.MethodGroup != null)
                {

                }
                // If the expression's type is invocable
                else
                {

                }
            });
            dependencyHandler.AddDisposable(target);
        }

        public void Dispose()
        {
            dependencyHandler.Dispose();
        }
    }
}