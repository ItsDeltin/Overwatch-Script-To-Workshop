namespace DS.Analysis.Scopes.Selector
{
    /// <summary>An IElementSelector implementation where the IdentifiedElement is known.</summary>
    class UnambiguousSelector : IElementSelector
    {
        readonly IdentifiedElement element;
        public UnambiguousSelector(IdentifiedElement element) => this.element = element;
        public IdentifiedElement GetIdentifiedElement(RelatedElements relatedElements) => element;
        public void Alias(RelatedElements relatedElements, string alias, IScopeAppender scopeAppender)
        {
            scopeAppender.AddScopedElement(ScopedElement.CreateAlias(alias, element));
        }
    }
}