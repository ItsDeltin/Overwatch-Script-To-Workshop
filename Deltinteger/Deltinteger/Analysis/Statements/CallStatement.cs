namespace DS.Analysis.Statements
{
    class CallStatement : Statement
    {
        readonly MethodAnalysis analysis;

        public CallStatement(MethodAnalysis analysis)
        {
            this.analysis = AddDisposable(analysis);
        }
    }
}