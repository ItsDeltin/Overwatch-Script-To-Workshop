using System.Linq;
using System.Collections.Generic;

namespace DS.Analysis.Scopes
{
    class Scope
    {
        IEnumerable<IScopeSource> _sources;

        public Scope()
        {
            _sources = Enumerable.Empty<IScopeSource>();
        }

        public Scope(IScopeSource source)
        {
            _sources = new[] { source };
        }

        public Scope(Scope parent, IScopeSource source)
        {
            _sources = parent._sources.Append(source);
        }

        public ScopeWatcher Watch()
        {
            ScopeWatcher watcher = new ScopeWatcher();
            
            foreach (var source in _sources)
                watcher.SubscribeTo(source);
            
            return watcher;
        }

        public Scope CreateChild(IScopeSource scopeSource) => new Scope(this, scopeSource);

        public static readonly Scope Empty = new Scope();
    }
}