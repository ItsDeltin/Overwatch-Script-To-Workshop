using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Animation
{
    public static class AnimationTestingFunctions
    {
        public static FuncMethod Rotate => new FuncMethodBuilder() {
            Name = "AnimTest_Rotate",
            Parameters = new CodeParameter[] {
                new CodeParameter("point"),
                new CodeParameter("axis"),
                new CodeParameter("angle")
            },
            Action = (actionSet, methodCall) => {
                var quat = actionSet.SaveValue("converted_quaternion", AnimationOperations.QuaternionFromAxis(methodCall.Get(1), methodCall.Get(2)), false);
                // return AnimationOperations.RotatePoint(actionSet, methodCall.Get(0), quat);

                var m3x3 = actionSet.SaveValue("m3x3", AnimationOperations.Create3x3MatrixFromQuaternion(quat), false);
                return AnimationOperations.MultiplyMatrix3x3AndVectorToVector(m3x3, methodCall.Get(0));
            },
            Documentation = "Rotation test"
        };
    }
}