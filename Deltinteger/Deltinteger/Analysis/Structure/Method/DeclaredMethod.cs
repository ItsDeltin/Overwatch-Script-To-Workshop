namespace DS.Analysis.Structure.Methods
{
    class DeclaredMethod : TypeDeclaredElement
    {
        readonly IMethodContentProvider contentProvider;
        Parameter[] parameters;

        public DeclaredMethod(ContextInfo contextInfo, IMethodContentProvider contentProvider)
        {
            this.contentProvider = contentProvider;
            Name = contentProvider.GetName();

            var setup = contentProvider.Setup(contextInfo);
            Type = setup.ReturnType;
            parameters = setup.Parameters;
        }


        public override void Dispose()
        {
            base.Dispose();
            contentProvider.Dispose();
        }
    }
}