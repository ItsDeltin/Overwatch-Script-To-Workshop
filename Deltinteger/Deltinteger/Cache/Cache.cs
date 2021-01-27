using System;
using System.Linq;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Cache
{
    static class Cache
    {
        // The list of cached values.
        private static readonly List<CacheElement> _cacheElements = new List<CacheElement>();

        public static CacheElement Get(ICacheIdentifier cacheIdentifier)
        {
            // Get the cached value from the identifier.
            foreach (var element in _cacheElements)
                if (element.Identifier.Matches(cacheIdentifier))
                    return element;

            // If the cached value does not exist, create it.
            var newElement = new CacheElement(cacheIdentifier);
            _cacheElements.Add(newElement);
            return newElement;
        }

        // Removes a cached value.
        public static void RemoveElement(CacheElement element) => _cacheElements.Remove(element);
    }

    class CacheElement
    {
        ///<summary>The cached value identifier.</summary>
        public ICacheIdentifier Identifier { get; }
        ///<summary>The cached value's actual value.</summary>
        public object Value { get; }
        ///<summary>A list of objects listening to this cached value.</summary>
        private readonly HashSet<ICacheWatcher> _watchers = new HashSet<ICacheWatcher>();

        public CacheElement(ICacheIdentifier identifier)
        {
            Identifier = identifier;
            Value = identifier.GetValue();
        }

        public void AddWatcher(ICacheWatcher watcher) => _watchers.Add(watcher);
        public void RemoveWatcher(ICacheWatcher watcher)
        {
            // Remove the watcher.
            _watchers.Remove(watcher);

            // If there are no more watchers, remove this from the cache list.
            if (_watchers.Count == 0)
                Cache.RemoveElement(this);
        }
    }
}