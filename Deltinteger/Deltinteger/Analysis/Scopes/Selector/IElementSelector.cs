namespace DS.Analysis.Scopes.Selector
{
    /// <summary>
    /// This interface is a component of `ScopedElement` which acts as an intermediary between operations
    /// wielding a `ScopedElement` and the useful `IdentifiedElement` data.
    /// Intuitively a `ScopedElement` would point directly to the `IdentifiedElement` (which contains
    /// data about the method, variable, type, module, etc.) However, this wouldn't work when an
    /// identifier points to multiple method overloads.
    /// <br />
    /// This is responsible for two tasks. The first is creating the `IdentifiedElement` data from
    /// the `RelatedElements` context. The second is aliasing elements imported from another file.
    /// <br />
    /// There are currently 2 implementations, `UnambiguousSelector` and `MethodGroupSelector`.
    /// </summary>
    interface IElementSelector
    {
        IdentifiedElement GetIdentifiedElement(RelatedElements relatedElements);

        void Alias(RelatedElements relatedElements, string alias, IScopeAppender scopeAppender);
    }
}