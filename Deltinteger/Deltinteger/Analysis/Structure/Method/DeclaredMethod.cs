namespace DS.Analysis.Structure.Methods
{
    class DeclaredMethod : TypeDeclaredElement
    {
        readonly IMethodContentProvider contentProvider;
        Parameter[] parameters;

        public DeclaredMethod(StructureContext structure, IMethodContentProvider contentProvider)
        {
            this.contentProvider = contentProvider;
            Name = contentProvider.GetName();
            contentProvider.GetStructure(structure);
        }

        public override void GetMeta(ContextInfo metaContext)
        {
            base.GetMeta(metaContext);
            Type = contentProvider.GetType(metaContext);
            parameters = contentProvider.GetParameters(metaContext);
            contentProvider.GetMeta(metaContext);
        }

        public override void GetContent() => contentProvider.GetContent();

        public override void Dispose()
        {
            base.Dispose();
            contentProvider.Dispose();
        }
    }
}