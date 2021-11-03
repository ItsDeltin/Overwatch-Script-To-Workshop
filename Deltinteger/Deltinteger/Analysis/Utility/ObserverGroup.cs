using System;
using System.Collections.Generic;

namespace DS.Analysis.Utility
{
    interface IObservable
    {
        IDisposable Watch(IObserver observer);
    }
    
    interface IObserver
    {
        void Update();
        void Complete();
    }

    class DisposeAction : IDisposable
    {
        readonly Action _action;
        public DisposeAction(Action action) => _action = action;
        public void Dispose() => _action();
    }

    class ObserverCollection : IDisposable
    {
        readonly List<IObserver> _observers = new List<IObserver>();

        public IDisposable Add(IObserver observer)
        {
            _observers.Add(observer);
            return new DisposeAction(() => _observers.Remove(observer));
        }

        public void Set()
        {
            foreach (var observer in _observers)
                observer.Update();
        }

        public void Dispose()
        {
            foreach (var observer in _observers)
                observer.Complete();
        }
    }

    class ObserverCollection<T> : IDisposable
    {
        readonly List<IObserver<T>> _observers = new List<IObserver<T>>();

        public IDisposable Add(IObserver<T> observer)
        {
            _observers.Add(observer);
            return new DisposeAction(() => _observers.Remove(observer));
        }

        public void Set(T value)
        {
            foreach (var observer in _observers)
                observer.OnNext(value);
        }

        public void Complete()
        {
            foreach (var observer in _observers)
                observer.OnCompleted();
        }

        public bool Any() => _observers.Count != 0;

        void IDisposable.Dispose() => Complete();
    }
}