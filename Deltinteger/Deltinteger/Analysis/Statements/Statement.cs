using DS.Analysis.Scopes;

namespace DS.Analysis.Statements
{
    abstract class Statement : Node
    {
        public virtual IScopeSource AddSourceToContext() => null;
    }
}