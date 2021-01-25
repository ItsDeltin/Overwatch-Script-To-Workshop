using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        public static FuncMethod Pi(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "Pi",
            Documentation = "Represents the ratio of the circumference of a circle to its diameter, specified by the constant π. Equal to `3.1415926535897931`.",
            ReturnType = deltinScript.Types.Number(),
            Action = (actionSet, methodCall) => Element.Num(Math.PI)
        };
    }
}