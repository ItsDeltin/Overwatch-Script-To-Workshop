using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger
{
    public interface IWorkshopTree
    {
        string ToWorkshop(OutputLanguage language, ToWorkshopContext context);
        bool EqualTo(IWorkshopTree other);
        int ElementCount() => 1;
    }

    public enum ToWorkshopContext
    {
        Action,
        ConditionValue,
        NestedValue,
        Other
    }
}