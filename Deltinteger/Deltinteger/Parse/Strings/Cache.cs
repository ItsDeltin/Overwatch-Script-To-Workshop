using System;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse.Strings
{
    class StringSaverComponent : IComponent, IDisposable
    {
        public DeltinScript DeltinScript { get; set; }
        public List<IStringParse> Strings { get; } = new List<IStringParse>();
        
        public void Init() {}

        public void Dispose() => RemoveUnused(Strings);

        private static readonly object _cacheLock = new object();
        private static readonly List<IStringParse> _cache = new List<IStringParse>();

        public static IStringParse GetCachedString(string str, bool localized)
        {
            lock (_cacheLock)
                foreach(var cachedString in _cache)
                    if (cachedString.Original == str && ((localized && cachedString is LocalizedString) || (!localized && cachedString is CustomStringGroup)))
                        return cachedString;
            return null;
        }

        public static void Add(IStringParse str)
        {
            lock (_cacheLock)
                _cache.Add(str);
        }

        static void RemoveUnused(List<IStringParse> strings)
        {
            lock (_cacheLock)
                for (int i = _cache.Count - 1; i >= 0; i--)
                    if (!strings.Contains(_cache[i]))
                        _cache.RemoveAt(i);
        }
    }
}