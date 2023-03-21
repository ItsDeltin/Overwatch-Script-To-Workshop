using System;

namespace DS.Analysis.Expressions
{
    using Types;

    class UnknownExpression : Expression
    {
        public UnknownExpression(ContextInfo context, IDisposable? warning = null) : base(context)
        {
            Type = StandardType.Unknown.Instance;

            if (warning != null)
                AddDisposable(warning);
        }
    }
}