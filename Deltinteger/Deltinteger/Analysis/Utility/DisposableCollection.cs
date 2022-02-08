using System;
using System.Collections.Generic;

namespace DS.Analysis.Utility
{
    /// <summary>A disposable that contains a list of disposables. When disposed, the internal list is disposed.
    /// Will throw ObjectDisposedException if disposed repeatively.</summary>
    class DisposableCollection : IDisposable
    {
        readonly HashSet<IDisposable> disposables = new HashSet<IDisposable>();
        private bool disposedValue;

        public void Add(IDisposable disposable)
        {
            disposables.Add(disposable);
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