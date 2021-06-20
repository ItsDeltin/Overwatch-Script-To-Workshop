using System;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Cache
{
    public interface ICacheWatcher
    {
        T Get<T>(ICacheIdentifier cacheIdentifier);
    }

    public class CacheWatcher : ICacheWatcher
    {
        public static readonly CacheWatcher Global = new CacheWatcher();

        private readonly HashSet<CacheElement> _savedElements = new HashSet<CacheElement>();
        private readonly HashSet<CacheElement> _currentCycle = new HashSet<CacheElement>();

        public void Unregister()
        {
            foreach (var element in _savedElements)
                element.RemoveWatcher(this);
        }

        public T Get<T>(ICacheIdentifier cacheIdentifier)
        {
            // Get the cached element.
            var element = Cache.Get(cacheIdentifier);

            element.AddWatcher(this);

            // Add the element to the savedElements and currentCycle.
            _savedElements.Add(element);
            _currentCycle.Add(element);

            return (T)element.Value;
        }

        public void EndCycle()
        {
            // Loop through each element.
            foreach (var savedElement in _savedElements)
                // If the saved element is not in the current cycle, remove it.
                if (!_currentCycle.Contains(savedElement))
                    savedElement.RemoveWatcher(this);

            _currentCycle.Clear();
        }
    }
}