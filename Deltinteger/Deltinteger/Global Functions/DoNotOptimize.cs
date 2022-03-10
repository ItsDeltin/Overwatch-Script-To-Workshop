namespace Deltin.Deltinteger.GlobalFunctions
{
    using Elements;
    using Parse;

    partial class GlobalFunctions
    {
        public static FuncMethod DoNotOptimize(DeltinScript deltinScript)
        {
            // Create a new FuncMethod.
            var builder = new FuncMethodBuilder()
            {
                Name = "DoNotOptimize",
                Documentation = "If optimization is enabled, ostw will not optimize any value passed to this method.",
                Action = (actionSet, methodCall) => StructHelper.BridgeIfRequired(methodCall.ParameterValues[0], value =>
                {
                    ((Element)value).Optimize = false;
                    return value;
                }),
            };

            MakeSharedParameterAndReturnType(ref builder, "The value that will not be optimized by the ostw compiler");

            return builder;
        }
    }
}