using System.Collections.Generic;

namespace DS.Analysis.Scopes.Selector
{
    /// <summary>
    /// When filtering a scope to select an identifier, this is passed to the ScopedElement's IElementSelector
    /// so that methods can be batched into method groups if need be. All Elements will have the same name.
    /// </summary>
    struct RelatedElements
    {
        public readonly IEnumerable<ScopedElement> Elements;

        public RelatedElements(IEnumerable<ScopedElement> elements)
        {
            Elements = elements;
        }
    }
}