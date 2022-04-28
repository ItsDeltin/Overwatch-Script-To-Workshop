using DS.Analysis.Types;

namespace DS.Analysis.Expressions
{
    class Number : Expression
    {
        readonly double value;

        public Number(ContextInfo context, double value) : base(context)
        {
            this.value = value;
            Type = StandardType.Number.Instance;
        }
    }
}