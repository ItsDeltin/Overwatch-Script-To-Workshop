namespace DS.Analysis.Expressions
{
    using Types.Standard;

    class UnknownExpression : Expression
    {
        public UnknownExpression(ContextInfo context) : base(context)
        {
            PhysicalType = StandardTypes.Unknown.Instance;
        }
    }
}