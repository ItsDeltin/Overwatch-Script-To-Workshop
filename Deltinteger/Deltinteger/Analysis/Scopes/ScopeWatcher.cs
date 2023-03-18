using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;
using DS.Analysis.Utility;
using DS.Analysis.Core;

namespace DS.Analysis.Scopes
{
    class ScopeWatcher : IScopeSource
    {
        // IScopeSource
        public ScopedElement[] Elements { get; private set; }

        readonly DSAnalysis master;
        readonly IEnumerable<IScopeSource> sources;
        readonly SingleNode node;

        public ScopeWatcher(DSAnalysis master, IEnumerable<IScopeSource> sources)
        {
            this.master = master;
            this.sources = sources;
            node = master.SingleNode("scopewatcher", () =>
            {
                // Concat all sources
                var result = Enumerable.Empty<ScopedElement>();
                foreach (var source in sources)
                    result = result.Concat(source.Elements);

                Elements = result.ToArray();

                node.MakeDependentsStale();
            });

            foreach (var source in sources)
                node.DependOn(source);
        }

        public ScopeWatcher CreateChild(IScopeSource scopeSource) => new ScopeWatcher(master, sources.Append(scopeSource));

        // IDependable
        public IDisposable AddDependent(IDependent dependent) => node.AddDependent(dependent);
    }
}