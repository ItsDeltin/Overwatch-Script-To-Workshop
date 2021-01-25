using System;
using System.Linq;
// using Deltin.Deltinteger.FileManager;
using Deltin.Deltinteger.Cache;

namespace Deltin.Deltinteger.Pathfinder
{
    class PathmapLoader : LoadedFile
    {
        public Pathmap Pathmap { get; private set; }

        public PathmapLoader(Uri uri) : base(uri) {}
        protected override void Update() => Pathmap = Pathmap.ImportFromText(GetContent());
    }

    class CompressedBakeCacheObject : ICacheIdentifier
    {
        private readonly Pathmap _map;
        private readonly int[] _attributes;

        public CompressedBakeCacheObject(Pathmap map, int[] attributes)
        {
            _map = map;
            _attributes = attributes;
        }

        public object GetValue() => CompressedBakeComponent.Create(_map, _attributes);

        public bool Matches(ICacheIdentifier other) => other is CompressedBakeCacheObject pathmapCacheObject
            && _map == pathmapCacheObject._map
            && _attributes.SequenceEqual(pathmapCacheObject._attributes);
    }
}