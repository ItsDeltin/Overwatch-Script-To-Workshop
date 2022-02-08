namespace DS.Analysis.Statements
{
    using Methods;

    class CallStatement : Statement
    {
        readonly MethodAnalysis analysis;

        public CallStatement(ContextInfo context, MethodAnalysis analysis) : base(context)
        {
            this.analysis = AddDisposable(analysis);
        }
    }
}