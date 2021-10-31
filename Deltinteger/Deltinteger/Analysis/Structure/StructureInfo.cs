using DS.Analysis.Utility;

namespace DS.Analysis.Structure
{
    struct StructureInfo
    {
        public IScopeAppender ScopeAppender { get; }

        public StructureInfo(IScopeAppender scopeAppender)
        {
            ScopeAppender = scopeAppender;
        }
    }
}