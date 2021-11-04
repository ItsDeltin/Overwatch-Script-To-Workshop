namespace DS.Analysis.Variables.Builder
{
    class VariableBuilder
    {
        public string Name { get; }
        protected IVariableContextHandler ContextHandler { get; }

        public VariableBuilder(IVariableContextHandler contextHandler)
        {
            ContextHandler = contextHandler;
            Name = contextHandler.GetName();
        }

        public VariableProvider GetVariable(ContextInfo contextInfo)
        {
            var result = new VariableProvider(Name, ContextHandler.GetTypeDirector(contextInfo));
            return result;
        }
    }
}