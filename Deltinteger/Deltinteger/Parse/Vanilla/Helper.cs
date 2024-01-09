#nullable enable
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Vanilla;

static class VanillaHelper
{
    public static string GetTypeOfWorkshopFunction(ElementBaseJson item) => item switch
    {
        ElementJsonValue => "value",
        ElementJsonAction => "action",
        _ => "function"
    };
}