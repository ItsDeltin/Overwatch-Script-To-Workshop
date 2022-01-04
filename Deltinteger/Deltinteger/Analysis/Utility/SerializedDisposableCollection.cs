using System;
using System.Collections.Generic;

namespace DS.Analysis.Utility
{
    /// <summary>A disposable that contains a list of disposables. When disposed, the internal list is disposed and cleared. Can be disposed repeatively.</summary>
    class SerializedDisposableCollection : IDisposable
    {
        readonly List<IDisposable> disposables = new List<IDisposable>();


        public SerializedDisposableCollection()
        {
            disposables = new List<IDisposable>();
        }

        public SerializedDisposableCollection(int capacity)
        {
            disposables = new List<IDisposable>(capacity);
        }


        public void Add(IDisposable item) => disposables.Add(item);

        public void Dispose()
        {
            disposables.Dispose();
            disposables.Clear();
        }
    }
}