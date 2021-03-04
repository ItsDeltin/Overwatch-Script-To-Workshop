namespace Deltin.Deltinteger.Parse
{
    public interface IParameterLike
    {
        string GetLabel(DeltinScript deltinScript, AnonymousLabelInfo labelInfo);
    }
}