namespace DS.Analysis.Expressions
{
    class CallExpression : Expression
    {
        readonly MethodAnalysis analysis;

        public CallExpression(MethodAnalysis analysis)
        {
            this.analysis = AddDisposable(analysis);
        }
    }
}