using System;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Methods
{
    using Expressions;

    class MethodAnalysis : IDisposable
    {
        readonly Expression target;
        readonly IDisposable targetSubscription;

        public MethodAnalysis(ContextInfo contextInfo, FunctionExpression syntax)
        {
            target = contextInfo.GetExpression(syntax.Target);
            targetSubscription = target.Subscribe(exprData =>
            {
                // If the expression is a method group
                if (exprData.MethodGroup != null)
                {

                }
                // If the expression's type is invocable
                else
                {

                }
            });
        }

        public void Dispose()
        {
            target.Dispose();
        }
    }
}