using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        public static FuncMethod Midpoint(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "Midpoint",
            Documentation = "The midpoint between 2 vectors.",
            Parameters = new[] {
                new CodeParameter("value1", "The first value.", NumberOrVector(deltinScript)),
                new CodeParameter("value2", "The second value.", NumberOrVector(deltinScript))
            },
            ReturnType = NumberOrVector(deltinScript),
            Action = (actionSet, methodCall) => (methodCall.Get(0) + methodCall.Get(1)) / 2
        };

        public static FuncMethod LinearInterpolate(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "LinearInterpolate",
            Documentation = "Gets a point on a line with a fraction.",
            Parameters = new[] {
                new CodeParameter("value1", "The first value.", NumberOrVector(deltinScript)),
                new CodeParameter("value2", "The second value.", NumberOrVector(deltinScript)),
                new CodeParameter("fraction", "The fraction. 0 will return the first point, 1 will return the second point, 0.5 will return the midpoint, etc.", deltinScript.Types.Number())
            },
            ReturnType = NumberOrVector(deltinScript),
            Action = (actionSet, methodCall) => {
                Element p1 = methodCall.Get(0), p2 = methodCall.Get(1), fraction = methodCall.Get(2);
                return (p1 * (1 - fraction)) + (p2 * fraction);
            }
        };

        public static FuncMethod LinearInterpolateDistance(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "LinearInterpolateDistance",
            Documentation = "Gets a point on a line by distance.",
            Parameters = new[] {
                new CodeParameter("value1", "The first value.", NumberOrVector(deltinScript)),
                new CodeParameter("value2", "The second value.", NumberOrVector(deltinScript)),
                new CodeParameter("distance", "The distance along the line.", deltinScript.Types.Number())
            },
            ReturnType = NumberOrVector(deltinScript),
            Action = (actionSet, methodCall) => {
                Element p1 = methodCall.Get(0), p2 = methodCall.Get(1), distance = methodCall.Get(2);
                Element fraction = distance / Element.DistanceBetween(p1, p2);
                return p1 * (1 - fraction) + (p2 * fraction);
            }
        };
    }
}