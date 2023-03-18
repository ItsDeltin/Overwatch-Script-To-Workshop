using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace DS.Analysis.Scopes
{
    using Core;

    class Scope : IDependable, IDisposable
    {
        public ScopedElement[] Elements { get; private set; }

        readonly IEnumerable<IScopeSource> sources;
        readonly SingleNode node;

        public Scope()
        {
            sources = Enumerable.Empty<IScopeSource>();
        }

        public Scope(DSAnalysis master, params IScopeSource[] sources)
        {
            this.sources = sources;
            node = master.SingleNode("scope", Update);
            Subscribe();
        }

        private Scope(DSAnalysis master, Scope parent, IScopeSource source)
        {
            sources = parent.sources.Append(source);
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

            Elements = elements.ToArray();
            node.MakeDependentsStale();
        }

        public Scope CreateChild(DSAnalysis master, IScopeSource scopeSource) => new Scope(master, this, scopeSource);

        // IDependable
        public IDisposable AddDependent(IDependent dependent) => node.AddDependent(dependent);

        public void Dispose() => node.Dispose();

        public static readonly Scope Empty = new Scope();
    }
}