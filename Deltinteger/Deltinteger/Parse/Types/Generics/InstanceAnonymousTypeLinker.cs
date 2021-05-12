using System.Linq;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public class InstanceAnonymousTypeLinker
    {
        // Represents an empty type linker.
        public static readonly InstanceAnonymousTypeLinker Empty = new InstanceAnonymousTypeLinker();
        public Dictionary<AnonymousType, CodeType> Links { get; } = new Dictionary<AnonymousType, CodeType>();

        public InstanceAnonymousTypeLinker(AnonymousType[] typeArgs, CodeType[] generics)
        {
            for (int i = 0; i < typeArgs.Length; i++)
                Links.Add(typeArgs[i], generics[i]);
        }

        public InstanceAnonymousTypeLinker() {}

        public CodeType[] TypeArgsFromAnonymousTypes(AnonymousType[] anonymousTypes) => (from a in anonymousTypes select Links[a]).ToArray();

        public InstanceAnonymousTypeLinker CloneMerge(InstanceAnonymousTypeLinker other) => new InstanceAnonymousTypeLinker(this, other);

        private InstanceAnonymousTypeLinker(InstanceAnonymousTypeLinker a, InstanceAnonymousTypeLinker b)
        {
            Links = new Dictionary<AnonymousType, CodeType>(a.Links);

            foreach (var pair in b.Links)
                Links.Add(pair.Key, pair.Value);
        }
    }
}