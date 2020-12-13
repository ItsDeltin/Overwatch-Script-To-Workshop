using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        public static FuncMethod MinWait() => new FuncMethodBuilder() {
            Name = "MinWait",
            Documentation = $"Waits for {Constants.MINIMUM_WAIT} seconds, the lowest allowed by the workshop. Equivalent to 1/60.",
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Element.Wait());
                return null;
            }
        };
    }
}