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

    class SerialScopeSource : IScopeSource
    {
        /// <summary>
        /// The elements in the scope. When changed, the scope dependents are marked as stale.
        /// </summary>
        public ScopedElement[] Elements
        {
            get => _elements; set
            {
                _elements = value ?? throw new NullReferenceException();
                dependentCollection.MarkAsStale();
            }
        }
        // Backing variable
        ScopedElement[] _elements = new ScopedElement[0];

        readonly DependencyList dependentCollection = new DependencyList("SerialScopeSource");

        public IDisposable AddDependent(IDependent dependent) => dependentCollection.Add(dependent);

        public SerialScopeSource() { }

        public SerialScopeSource(ScopedElement[] initialElements) => _elements = initialElements;
    }

    class ScopeSource : IScopeSource, IScopeAppender
    {
        public ScopedElement[] Elements => scopedElements.ToArray();

        readonly List<ScopedElement> scopedElements = new List<ScopedElement>();
        readonly DependencyList dependents = new DependencyList("ScopeSource");

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

    class EmptyScopeSource : IScopeSource
    {
        public static readonly EmptyScopeSource Instance = new EmptyScopeSource();

        private EmptyScopeSource() { }

        public ScopedElement[] Elements { get; } = new ScopedElement[0];
        public IDisposable AddDependent(IDependent dependent) => System.Reactive.Disposables.Disposable.Empty;
    }
}