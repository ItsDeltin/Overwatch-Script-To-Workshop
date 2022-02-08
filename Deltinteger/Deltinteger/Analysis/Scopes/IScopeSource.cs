using System;
using System.Linq;
using System.Collections.Generic;
using DS.Analysis.Utility;

namespace DS.Analysis.Scopes
{
    using Core;

    // Contains a source of declarations that can be accessed.
    interface IScopeSource : IDependable
    {
        ScopedElement[] Elements { get; }
    }

    class ScopeSource : IScopeSource, IScopeAppender
    {
        public ScopedElement[] Elements => scopedElements.ToArray();

        readonly List<ScopedElement> scopedElements = new List<ScopedElement>();
        readonly DependentCollection dependents = new DependentCollection();

        public ScopeSource() { }

        public IDisposable AddDependent(IDependent dependent) => dependents.Add(dependent);

        public void Clear()
        {
            scopedElements.Clear();
            dependents.MarkAsStale();
        }

        public void AddScopedElement(ScopedElement element)
        {
            scopedElements.Add(element);
            dependents.MarkAsStale();
        }
    }

    struct ScopeSourceChange
    {
        public static readonly ScopeSourceChange Empty = new ScopeSourceChange(new ScopedElement[0]);

        public readonly ScopedElement[] Elements;

        public ScopeSourceChange(ScopedElement[] elements) => Elements = elements;

        public ScopeSourceChange(IEnumerable<ScopedElement> elements) => Elements = elements.ToArray();
    }
}