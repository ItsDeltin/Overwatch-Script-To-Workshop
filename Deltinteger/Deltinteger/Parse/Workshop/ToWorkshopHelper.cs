namespace Deltin.Deltinteger.Parse;
using Elements;

static class ToWorkshopHelper
{
    public static IWorkshopTree GuardArrayModifiedValue(IWorkshopTree value, CodeType type)
    {
        if (type.NeedsArrayProtection)
        {
            return Element.CreateArray(value);
        }
        return value;
    }
}