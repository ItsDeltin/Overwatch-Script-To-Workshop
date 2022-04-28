using System.Linq;
using System.Collections.Generic;

namespace DS.Analysis.Scopes.Selector
{
    using Methods;

    /// <summary>An implementation of IElementSelector that generates a method group when a method is identified.</summary>
    class MethodGroupSelector : IElementSelector
    {
        public IdentifiedElement GetIdentifiedElement(RelatedElements relatedElements)
        {
            // Create the method group.
            var overloads = new HashSet<MethodInstance>();

            // Add methods with a matching name.
            foreach (var relatedElement in relatedElements.Elements)
                // todo: check for conflicts
                if (relatedElement.Method != null)
                    overloads.Add(relatedElement.Method);

            return new IdentifiedElement(new MethodGroup(overloads.ToArray()));
        }

        public void Alias(RelatedElements relatedElements, string alias, IScopeAppender scopeAppender)
        {
            // Add a scoped element for every overload.
            foreach (var relatedElement in relatedElements.Elements)
                if (relatedElement.Method != null)
                    scopeAppender.AddScopedElement(ScopedElement.CreateMethod(alias, relatedElement.Method));
        }
    }
}