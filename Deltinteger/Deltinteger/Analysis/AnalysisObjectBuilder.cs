using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace DS.Analysis
{
    using Core;

    class AnalysisObjectBuilder
    {

        readonly List<IDisposable> disposables = new List<IDisposable>();

        readonly IMaster master;

        public AnalysisObjectBuilder(IMaster master)
        {
            this.master = master;
        }

        public IDisposable AddDisposable(IDisposable disposable)
        {
            disposables.Add(disposable);

            return Disposable.Create(() =>
            {
                if (disposables.Remove(disposable))
                    disposable.Dispose();
            });
        }

        public void CreateNode(Action<NodeHelper> action)
        {

        }

        public void AddDependent()
        {

        }

        public AnalysisObjectBuilder()
        {

        }
    }

    class AnalysisObjectNode : IDependent, IUpdatable
    {
        readonly IMaster master;
        readonly Action action;

        public AnalysisObjectNode(IMaster master, Action action)
        {
            (this.master, this.action) = (master, action);
            MarkAsStale();
        }

        public void MarkAsStale() => master.AddStaleObject(this);

        public void Update() => action();

        public void DependOn()
        {

        }
    }

    class NodeHelper
    {
        public void DependOn()
        {
        }
    }
}