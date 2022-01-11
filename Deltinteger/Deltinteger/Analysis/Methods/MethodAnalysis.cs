using System;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Methods
{
    using Expressions;

    class MethodAnalysis : IDisposable
    {
        readonly Expression target;

        public MethodAnalysis(ContextInfo contextInfo, FunctionExpression syntax)
        {
            target = contextInfo.GetExpression(syntax.Target);
        }

        public void Dispose()
        {
            target.Dispose();
        }
    }
}