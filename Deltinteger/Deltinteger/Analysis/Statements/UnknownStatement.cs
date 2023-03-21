namespace DS.Analysis.Statements;
using System;

class UnknownStatement : Statement
{
    public UnknownStatement(ContextInfo context, IDisposable? warning) : base(context)
    {
        if (warning != null)
            this.AddDisposable(warning);
    }
}