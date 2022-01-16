using System;
using System.Linq;
using System.Collections.Generic;
using DS.Analysis.Utility;

namespace DS.Analysis.Scopes
{
    // Contains a source of declarations that can be accessed.
    interface IScopeSource : IObservable<ScopeSourceChange>
    {
    }

    class ScopeSource : IScopeSource, IScopeAppender
    {
        readonly List<ScopedElement> _scopedElements = new List<ScopedElement>();
        readonly ObserverCollection<ScopeSourceChange> _subscribers = new ObserverCollection<ScopeSourceChange>();

        public ScopeSource() { }

        public IDisposable Subscribe(IObserver<ScopeSourceChange> observer)
        {
            observer.OnNext(new ScopeSourceChange(_scopedElements.ToArray()));
            return _subscribers.Add(observer);
        }

        public void Clear()
        {
            _scopedElements.Clear();
        }

        public void Refresh() => _subscribers.Set(new ScopeSourceChange(_scopedElements.ToArray()));

        public void AddScopedElement(ScopedElement element)
        {
            _scopedElements.Add(element);
            Refresh();
        }
    }

    struct ScopeSourceChange
    {
        public static readonly ScopeSourceChange Empty = new ScopeSourceChange(new ScopedElement[0]);

        public readonly ScopedElement[] Elements;

        public ScopeSourceChange(ScopedElement[] elements) => Elements = elements;

        public ScopeSourceChange(IEnumerable<ScopedElement> elements) => Elements = elements.ToArray();
    }
}