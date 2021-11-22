using DS.Analysis.Types.Standard;

namespace DS.Analysis.Expressions
{
    class Number : Expression
    {
        readonly double value;

        public Number(double value)
        {
            this.value = value;
            Type = StandardTypes.Number.Director;
        }
    }
}