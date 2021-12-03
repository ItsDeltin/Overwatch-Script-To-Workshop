namespace DS.Analysis.Expressions
{
    using Types.Standard;

    class UnknownExpression : Expression
    {
        public UnknownExpression()
        {
            SetTypeDirector(StandardTypes.Unknown.Director);
        }
    }
}