using System.Linq;
using System.Collections.Generic;

namespace DS.Analysis.Scopes
{
    class Scope
    {
        IEnumerable<ScopeSource> _sources;

        public Scope(ScopeSource source)
        {
            _sources = new[] { source };
        }

        public Scope(Scope parent, ScopeSource source)
        {
            _sources = parent._sources.Append(source);
        }

        public ScopeWatcher Watch(string name) => Watch(new ScopeWatcherParameters(name));

        public ScopeWatcher Watch(ScopeWatcherParameters parameters)
        {
            ScopeWatcher watcher = new ScopeWatcher(parameters);
            
            foreach (var source in _sources)
                watcher.SubscribeTo(source);
            
            return watcher;
        }

        public Scope CreateChild() => new Scope(this, new ScopeSource());
    }
}