namespace Deltin.Deltinteger.Parse
{
    public interface ITypeArgTrackee
    {
        AnonymousType[] GenericTypes { get; }
        int GenericsCount { get; }
    }
}