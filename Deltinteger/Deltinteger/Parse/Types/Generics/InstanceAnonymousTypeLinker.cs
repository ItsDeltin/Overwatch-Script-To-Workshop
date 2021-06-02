using System.Linq;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public class InstanceAnonymousTypeLinker
    {
        // Represents an empty type linker.
        public static InstanceAnonymousTypeLinker Empty => new InstanceAnonymousTypeLinker();

        // Pairs of type-args linked with type-values.
        public Dictionary<AnonymousType, CodeType> Links { get; } = new Dictionary<AnonymousType, CodeType>();

        // Creates a type linker from an array of type-args and an array of type-values.
        // An exception will be thrown if they are not the same length.
        public InstanceAnonymousTypeLinker(AnonymousType[] typeArgs, CodeType[] typeValues)
        {
            for (int i = 0; i < typeArgs.Length; i++)
                Links.Add(typeArgs[i], typeValues[i]);
        }

        // Empty linker.
        public InstanceAnonymousTypeLinker() {}

        // Extracts an array of type values from an array of type-args.
        // An exception will be thrown if a provided type-arg is not contained in the 'Links' dictionary.
        public CodeType[] TypeArgsFromAnonymousTypes(AnonymousType[] anonymousTypes) => (from a in anonymousTypes select Links[a]).ToArray();
        public CodeType[] SafeTypeArgsFromAnonymousTypes(AnonymousType[] anonymousTypes) => (from a in anonymousTypes select Links.TryGetValue(a, out var value) ? value : a).ToArray();

        // Creates a new linker with the current linker merged with the provided linker.
        // If a duplicate key is found in the provided linker, this is prioritized.
        public InstanceAnonymousTypeLinker CloneMerge(InstanceAnonymousTypeLinker other) => new InstanceAnonymousTypeLinker(this, other);

        // Adds a link between a type-arg and a type-value.
        public void Add(AnonymousType typeArg, CodeType typeValue) => Links.Add(typeArg, typeValue);

        // Private constructor for CloneMerge.
        private InstanceAnonymousTypeLinker(InstanceAnonymousTypeLinker a, InstanceAnonymousTypeLinker b)
        {
            Links = new Dictionary<AnonymousType, CodeType>(a.Links);

            foreach (var pair in b.Links)
                if (!Links.ContainsKey(pair.Key))
                    Links.Add(pair.Key, pair.Value);
        }
    }
}