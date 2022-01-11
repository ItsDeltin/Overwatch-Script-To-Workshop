namespace DS.Analysis.Expressions
{
    using Methods;

    class CallExpression : Expression
    {
        readonly MethodAnalysis analysis;

        public CallExpression(MethodAnalysis analysis)
        {
            this.analysis = AddDisposable(analysis);
        }
    }
}