using DS.Analysis.Scopes;

namespace DS.Analysis.Scopes
{
    interface IScopeAppender
    {
        void AddScopedElement(ScopedElement element);
    }
}