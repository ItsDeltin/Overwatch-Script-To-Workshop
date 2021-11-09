using System;

namespace DS.Analysis.Variables.Builder
{
    class VariableBuilder : IDisposable
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
            VariableContent content = ContextHandler.GetContent(contextInfo);

            return new VariableProvider(Name, content.TypeDirector);
        }

        public void Dispose() => ContextHandler.Dispose();
    }
}