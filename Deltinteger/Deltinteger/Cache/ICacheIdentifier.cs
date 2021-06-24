namespace Deltin.Deltinteger.Cache
{
    public interface ICacheIdentifier
    {
        bool Matches(ICacheIdentifier other);
        object GetValue();
    }
}