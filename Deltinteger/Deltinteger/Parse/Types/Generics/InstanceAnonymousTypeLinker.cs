using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public class InstanceAnonymousTypeLinker
    {
        public static readonly InstanceAnonymousTypeLinker Empty = new InstanceAnonymousTypeLinker(); 

        public Dictionary<AnonymousType, CodeType> Links { get; } = new Dictionary<AnonymousType, CodeType>();

        public InstanceAnonymousTypeLinker(AnonymousType[] typeArgs, CodeType[] generics)
        {
            for (int i = 0; i < typeArgs.Length; i++)
                Links.Add(typeArgs[i], generics[i]);
        }

        public InstanceAnonymousTypeLinker() {}
    }
}