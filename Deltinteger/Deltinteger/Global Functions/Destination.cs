using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        public static FuncMethod Destination(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "Destination",
            Documentation = "Calculates a destination given a starting point, distance and direction",
            Parameters = new[] {
                new CodeParameter("startingPoint", "The starting point.", deltinScript.Types.Vector()),
                new CodeParameter("direction", "The direction to move.", deltinScript.Types.Vector()),
                new CodeParameter("distance", "The distance to move.", deltinScript.Types.Number())
            },
            ReturnType = deltinScript.Types.Vector(),
            Action = (actionSet, methodCall) => methodCall.Get(0) + methodCall.Get(1) * methodCall.Get(2)
        };
    }
}