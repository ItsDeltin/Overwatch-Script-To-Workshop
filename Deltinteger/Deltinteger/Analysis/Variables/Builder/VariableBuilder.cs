namespace DS.Analysis.Variables.Builder
{
    class VariableBuilder
    {
        protected IVariableContextHandler ContextHandler { get; }

        public VariableBuilder(IVariableContextHandler contextHandler)
        {
            ContextHandler = contextHandler;
        }

        public VariableProvider GetVariable(ContextInfo contextInfo)
        {
            var result = new VariableProvider(ContextHandler.GetName(), ContextHandler.GetTypeDirector(contextInfo));
            return result;
        }
    }
}