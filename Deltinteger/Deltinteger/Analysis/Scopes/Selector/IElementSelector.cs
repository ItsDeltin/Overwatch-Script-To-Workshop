namespace DS.Analysis.Scopes.Selector
{
    interface IElementSelector
    {
        IdentifiedElement GetIdentifiedElement(RelatedElements relatedElements);

        void Alias(RelatedElements relatedElements, string alias, IScopeAppender scopeAppender);
    }
}