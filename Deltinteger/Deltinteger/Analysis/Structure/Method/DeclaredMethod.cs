namespace DS.Analysis.Structure.Methods
{
    using DS.Analysis.Scopes;
    using Types;
    using Types.Components;
    using Analysis.Methods;

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
            contentBuilder.AddElement(new MethodClassElement(CreateInstance()));
        }

        public override void AddToScope(IScopeAppender scopeAppender)
        {
            scopeAppender.AddScopedElement(ScopedElement.CreateMethod(CreateInstance()));
        }

        public override void Dispose()
        {
            contentProvider.Dispose();
        }


        MethodInstance CreateInstance()
        {
            return new MethodInstance(Name);
        }
    }
}