namespace DS.Analysis.Expressions
{
    using Types;

    class UnknownExpression : Expression
    {
        public UnknownExpression(ContextInfo context) : base(context)
        {
            Type = StandardType.Unknown.Instance;
        }
    }
}