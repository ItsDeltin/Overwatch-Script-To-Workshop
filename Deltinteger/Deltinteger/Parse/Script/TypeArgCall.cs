namespace Deltin.Deltinteger.Parse
{
    public struct TypeArgCall
    {
        public ITypeArgTrackee Trackee { get; }
        public CodeType[] TypeArgs { get; }

        public TypeArgCall(ITypeArgTrackee trackee, CodeType[] typeArgs)
        {
            Trackee = trackee;
            TypeArgs = typeArgs;
        }
    }
}