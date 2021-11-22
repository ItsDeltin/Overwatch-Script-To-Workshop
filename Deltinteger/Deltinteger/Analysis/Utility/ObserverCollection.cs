using System;
using System.Collections.Generic;

namespace DS.Analysis.Utility
{
    class DisposeAction : IDisposable
    {
        readonly Action _action;
        public DisposeAction(Action action) => _action = action;
        public void Dispose() => _action();
    }

    class ObserverCollection<T> : IDisposable
    {
        readonly List<IObserver<T>> _observers = new List<IObserver<T>>();

        public virtual IDisposable Add(IObserver<T> observer)
        {
            _observers.Add(observer);
            return new DisposeAction(() => _observers.Remove(observer));
        }

        public virtual void Set(T value)
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

    class ValueObserverCollection<T> : ObserverCollection<T>
    {
        public T Value { get; private set; }

        public ValueObserverCollection() { }

        public ValueObserverCollection(T initialValue) => Value = initialValue;

        public override IDisposable Add(IObserver<T> observer)
        {
            observer.OnNext(Value);
            return base.Add(observer);
        }

        public override void Set(T value)
        {
            Value = value;
            base.Set(value);
        }
    }
}