using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public interface IGenericUsage
    {
        void ApplyToTracker(TrackerTypeArg applyTo);
    }

    class AddToGenericsUsage : IGenericUsage
    {
        readonly CodeType _type;
        public AddToGenericsUsage(CodeType type) => _type = type;
        public void ApplyToTracker(TrackerTypeArg applyTo) => applyTo.UsedWithType(_type);
    }

    class BridgeAnonymousUsage : IGenericUsage
    {
        // A list of type arg trackers that this anonymous type is used within.
        private readonly HashSet<TrackerTypeArg> _usedWith;

        // A list of type args that are provided for this anonymous type.
        private readonly HashSet<IGenericUsage> _typeArgs;

        public void ApplyToTracker(TrackerTypeArg applyTo)
        {
            _usedWith.Add(applyTo); // Add the tracker to the list of trackers.

            // Set tracker with every type arg.
            foreach (var typeArg in _typeArgs)
                typeArg.ApplyToTracker(applyTo);
        }

        public void TypeArgProvided(IGenericUsage typeArg)
        {
            _typeArgs.Add(typeArg); // Add the typeArg to the list of type args.

            // Update trackers with the new type arg.
            foreach (var tracker in _usedWith)
                typeArg.ApplyToTracker(tracker);
        }
    }
}