namespace Deltin.Deltinteger.Parse
{
    public struct GetTypeName
    {
        public bool MakeAnonymousTypesUnknown;
        public InstanceAnonymousTypeLinker TypeLinker;

        public GetTypeName(bool makeAnonymousTypesUnknown, InstanceAnonymousTypeLinker typeLinker)
        {
            MakeAnonymousTypesUnknown = makeAnonymousTypesUnknown;
            TypeLinker = typeLinker;
        }
    }
}