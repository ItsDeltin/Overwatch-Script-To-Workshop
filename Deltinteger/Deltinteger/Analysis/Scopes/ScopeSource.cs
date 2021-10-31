using System;
using System.Collections.Generic;
using DS.Analysis.Utility;

namespace DS.Analysis.Scopes
{
    // Contains a source of declarations that can be accessed.
    abstract class AbstractScopeSource
    {
        // Watches for identifiers using the specified parameters.
        public abstract AbstractUnsubscriber Subscribe(IScopeSourceListener watcher);

        public abstract class AbstractUnsubscriber : IDisposable
        {
            public abstract void Dispose();
        }
    }

    class ScopeSource : AbstractScopeSource, IScopeAppender
    {
        readonly List<ScopedElement> _scopedElements = new List<ScopedElement>();
        readonly List<IScopeSourceListener> _subscribers = new List<IScopeSourceListener>();

        public override AbstractUnsubscriber Subscribe(IScopeSourceListener watcher)
        {
            AbstractUnsubscriber unsubscriber = new Unsubscriber(this, watcher);
            _subscribers.Add(watcher);

            foreach (var element in _scopedElements)
                watcher.Notify(element);

            return unsubscriber;
        }

        public void Clear()
        {
            _scopedElements.Clear();
        }

        public void AddScopedElement(ScopedElement element)
        {
            _scopedElements.Add(element);
            
            foreach (var subscriber in _subscribers)
                subscriber.Notify(element);
        }

        class Unsubscriber : AbstractUnsubscriber
        {
            readonly ScopeSource _source;
            readonly IScopeSourceListener _watcher;

            public Unsubscriber(ScopeSource source, IScopeSourceListener watcher)
            {
                _source = source;
                _watcher = watcher;
            }

            public override void Dispose() => _source._subscribers.Remove(_watcher);
        }
    }

    interface IScopeSourceListener
    {
        void Notify(ScopedElement element);
    }
}