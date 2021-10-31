namespace DS.Analysis.Structure.Methods
{
    abstract class AbstractMethodDeclaration : TypeDeclaredElement
    {
        public Parameter[] Parameters { get; protected set; }

        public IMethodContent Content { get; protected set; }
    }

    class GenericMethodDeclaration : AbstractMethodDeclaration
    {
        readonly AbstractMethodContentProvider _contentProvider;

        public GenericMethodDeclaration(AbstractMethodContentProvider contentProvider)
        {
            _contentProvider = contentProvider;
            Name = _contentProvider.GetName();
        }

        public override void GetMeta(ContextInfo metaContext)
        {
            Type = _contentProvider.GetType(metaContext);
            Parameters = _contentProvider.GetParameters(metaContext);
        }

        public override void GetContent(ContextInfo context)
        {
            Content = _contentProvider.GetContent(context);
        }
    }
}