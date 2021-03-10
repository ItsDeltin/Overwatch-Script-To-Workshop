using System;
using System.Linq;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public interface ITypeArgTrackee
    {
        int GenericsCount { get; }
    }

    /// <summary>Tracks the generic usage for all type providers.</summary>
    class TypeTrackerComponent : IComponent
    {
        // ProviderTrackerInfo linked to providers.
        public IReadOnlyDictionary<ITypeArgTrackee, ProviderTrackerInfo> Trackers => _trackers;
        readonly Dictionary<ITypeArgTrackee, ProviderTrackerInfo> _trackers = new Dictionary<ITypeArgTrackee, ProviderTrackerInfo>();

        public void Init(DeltinScript deltinScript) {}

        // Adds to the tracker.
        public void Track(ITypeArgTrackee provider, IGenericUsage[] generics)
        {
            // Make sure the number of generics in the provider and the number of generics in the parameter match.
            if (provider.GenericsCount != generics.Length)
                throw new Exception("Generic count do not match.");

            // Get the tracker for the provider.
            if (!_trackers.TryGetValue(provider, out ProviderTrackerInfo trackerInfo))
            {
                // If a tracker was not registered for the provider, create one.
                trackerInfo = ProviderTrackerInfo.FromTrackee(provider);
                _trackers.Add(provider, trackerInfo);
            }

            // Loop through each type arg and apply it.
            for (int i = 0; i < generics.Length; i++)
                generics[i].ApplyToTracker(trackerInfo.TypeArgTracker[i]);
        }

        public void Track(ITypeArgTrackee provider, CodeType[] generics)
            => Track(provider, generics.Select(g => g.GenericUsage).ToArray());
    }

    /// <summary>Tracks the generic usage for a type provider.</summary>
    public class ProviderTrackerInfo
    {
        public TrackerTypeArg[] TypeArgTracker { get; }

        public ProviderTrackerInfo(TrackerTypeArg[] typeArgs)
        {
            TypeArgTracker = typeArgs;
        }

        public static ProviderTrackerInfo FromTrackee(ITypeArgTrackee codeTypeProvider)
        {
            // Create a TrackerTypeArg for each generics.
            var arr = new TrackerTypeArg[codeTypeProvider.GenericsCount];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = new TrackerTypeArg();

            return new ProviderTrackerInfo(arr);
        }
    }

    public class TrackerTypeArg
    {
        public IReadOnlyList<CodeType> UsedTypes => _usedTypes;
        readonly List<CodeType> _usedTypes = new List<CodeType>();
        public void UsedWithType(CodeType type) => _usedTypes.Add(type);
    }
}