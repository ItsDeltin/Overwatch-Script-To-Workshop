namespace Deltin.Deltinteger.Parse.Strings
{
    public interface IStringParse
    {
        string Original { get; }
        int ArgCount { get; }
        IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameters);
    }
}