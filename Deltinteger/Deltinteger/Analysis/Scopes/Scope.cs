using System;
using System.Linq;
using System.Collections.Generic;

namespace DS.Analysis.Scopes
{
    using Core;

    class Scope : IDependable, IDisposable
    {
        private ScopedElement[] elements;

        readonly IEnumerable<IScopeSource> sources;
        readonly SingleNode node;

        private Scope(DSAnalysis master, IEnumerable<IScopeSource> sources)
        {
            this.sources = sources;
            node = master.SingleNode("scope", Update);
            Subscribe();
        }

        void Subscribe()
        {
            foreach (var source in sources)
                node.DependOn(source);
        }

        void Update()
        {
            var elements = Enumerable.Empty<ScopedElement>();

            foreach (var source in sources)
                elements = elements.Concat(source.Elements);

            this.elements = elements.ToArray();
            node.MakeDependentsStale();
        }

        /// <summary>When selecting a scoped element with an identifier, choose the last matching element.</summary>
        public ScopedElement[] GetScopedElements()
        {
            return elements;
        }

        public Scope CreateChild(DSAnalysis master, IScopeSource scopeSource)
        {
            var sources = this.sources.Append(scopeSource);
            return new Scope(master, sources);
        }

        public static Scope New(DSAnalysis master, params IScopeSource[] sources) => new Scope(master, sources);

        // IDependable
        public IDisposable AddDependent(IDependent dependent) => node.AddDependent(dependent);

        public void Dispose() => node.Dispose();
    }
}