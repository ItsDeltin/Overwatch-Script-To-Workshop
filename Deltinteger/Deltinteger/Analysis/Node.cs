using System;
using System.Collections.Generic;

namespace DS.Analysis
{
    abstract class Node : IDisposable
    {
        readonly List<IDisposable> _disposables = new List<IDisposable>();

        protected T AddDisposable<T>(T disposable) where T : IDisposable
        {
            _disposables.Add(disposable);
            return disposable;
        }

        public virtual void Dispose()
        {
            foreach (var disposable in _disposables)
                disposable.Dispose();
        }
    }
}