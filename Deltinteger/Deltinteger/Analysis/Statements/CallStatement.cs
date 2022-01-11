namespace DS.Analysis.Statements
{
    using Methods;

    class CallStatement : Statement
    {
        readonly MethodAnalysis analysis;

        public CallStatement(MethodAnalysis analysis)
        {
            this.analysis = AddDisposable(analysis);
        }
    }
}