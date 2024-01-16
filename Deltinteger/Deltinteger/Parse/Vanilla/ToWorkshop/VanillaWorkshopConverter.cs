#nullable enable
using Deltin.Deltinteger.Parse.Variables.VanillaLink;
namespace Deltin.Deltinteger.Parse.Vanilla.ToWorkshop;

struct VanillaWorkshopConverter
{
    public readonly LinkableVanillaVariables LinkedVariables;
    public IWorkshopTree? CurrentObject = default;

    public VanillaWorkshopConverter(LinkableVanillaVariables linkedVariables)
    {
        LinkedVariables = linkedVariables;
    }

    public readonly VanillaWorkshopConverter SetCurrentObject(IWorkshopTree? newValue)
    {
        var copy = this;
        copy.CurrentObject = newValue;
        return copy;
    }
}