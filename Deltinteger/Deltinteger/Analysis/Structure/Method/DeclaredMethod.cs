namespace DS.Analysis.Structure.Methods
{
    using DS.Analysis.Scopes;
    using Types;

    class DeclaredMethod : AbstractDeclaredElement
    {
        readonly IMethodContentProvider contentProvider;
        IDisposableTypeDirector returnTypeDirector;
        Parameter[] parameters;

        public DeclaredMethod(ContextInfo contextInfo, IMethodContentProvider contentProvider)
        {
            this.contentProvider = contentProvider;
            Name = contentProvider.GetName();

            var setup = contentProvider.Setup(contextInfo);
            returnTypeDirector = setup.ReturnType;
            parameters = setup.Parameters;
        }

        public override void AddToContent(TypeContentBuilder contentBuilder)
        {
            // todo
            throw new System.NotImplementedException();
        }

        public override void AddToScope(IScopeAppender scopeAppender)
        {
            scopeAppender.AddScopedElement(ScopedElement.Create(Name, null, null));
        }

        public override void Dispose()
        {
            contentProvider.Dispose();
        }
    }
}