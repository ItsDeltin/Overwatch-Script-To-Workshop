using DS.Analysis.Types;

namespace DS.Analysis.Structure.Methods
{
    abstract class AbstractMethodContentProvider
    {
        public abstract string GetName();
        public abstract Parameter[] GetParameters(ContextInfo metaContext);
        public abstract TypeReference GetType(ContextInfo metaContext);
        public abstract IMethodContent GetContent(ContextInfo context);
    }
}