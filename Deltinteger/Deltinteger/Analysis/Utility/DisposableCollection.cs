using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace DS.Analysis.Utility
{
    /// <summary>A disposable that contains a list of disposables. When disposed, the internal list is disposed.
    /// Will throw ObjectDisposedException if disposed repeatively.</summary>
    class DisposableCollection : IDisposable
    {
        readonly HashSet<IDisposable> disposables = new HashSet<IDisposable>();
        private bool disposedValue;

        public IDisposable Add(IDisposable disposable)
        {
            disposables.Add(disposable);
            return Disposable.Create(() =>
            {
                if (disposables.Remove(disposable))
                    disposable.Dispose();
            });
        }

        public void Dispose()
        {
            if (disposedValue)
                throw new ObjectDisposedException(ToString());

            disposedValue = true;
            disposables.Dispose();
        }
    }
}