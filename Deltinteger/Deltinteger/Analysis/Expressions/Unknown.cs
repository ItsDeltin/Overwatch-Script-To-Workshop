namespace DS.Analysis.Expressions
{
    using Types.Standard;

    class UnknownExpression : Expression
    {
        public UnknownExpression()
        {
            Type = StandardTypes.Unknown.Director;
        }
    }
}