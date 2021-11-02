using System;
using System.Collections.Generic;

namespace DS.Analysis
{
    abstract class Node : IDisposable
    {
        readonly List<IDisposable> _disposables = new List<IDisposable>();

        protected void AddDisposable(IDisposable disposable) => _disposables.Add(disposable);

        public virtual void Dispose()
        {
            foreach (var disposable in _disposables)
                disposable.Dispose();
        }


        public virtual void GetMeta(ContextInfo contextInfo) {}

        public virtual void GetContent(ContextInfo contextInfo) {}
    }
}