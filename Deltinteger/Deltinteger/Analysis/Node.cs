using System;
using System.Collections.Generic;

namespace DS.Analysis
{
    abstract class Node : IDisposable
    {
        protected ContextInfo ContextInfo { get; private set; }

        readonly List<IDisposable> _disposables = new List<IDisposable>();

        protected void AddDisposable(IDisposable disposable) => _disposables.Add(disposable);

        public virtual void Dispose()
        {
            foreach (var disposable in _disposables)
                disposable.Dispose();
        }


        public virtual void GetMeta(ContextInfo contextInfo)
        {
            ContextInfo = contextInfo;
        }

        public virtual void GetContent() {}
    }
}