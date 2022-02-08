using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;
using DS.Analysis.Utility;
using DS.Analysis.Core;

namespace DS.Analysis.Scopes
{
    class ScopeWatcher : AnalysisObject, IScopeSource
    {
        readonly List<IScopeSource> sources = new List<IScopeSource>();
        public ScopedElement[] Elements { get; private set; }
        readonly Action onEmpty;

        public ScopeWatcher(IMaster master, Action onEmpty) : base(master)
        {
            this.onEmpty = onEmpty;
        }

        public void SubscribeTo(IScopeSource scopeSource)
        {
            sources.Add(scopeSource);
            DependOn(scopeSource);
        }

        public override void Update()
        {
            base.Update();

            var result = Enumerable.Empty<ScopedElement>();
            foreach (var source in sources)
                result = result.Concat(source.Elements);

            Elements = result.ToArray();
        }

        protected override void NoMoreDependents() => onEmpty();
    }
}