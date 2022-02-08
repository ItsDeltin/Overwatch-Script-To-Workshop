namespace DS.Analysis.Expressions
{
    using Methods;

    class CallExpression : Expression
    {
        readonly MethodAnalysis analysis;

        public CallExpression(ContextInfo context, MethodAnalysis analysis) : base(context)
        {
            this.analysis = AddDisposable(analysis);
        }
    }
}