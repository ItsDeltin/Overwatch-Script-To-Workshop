using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        // EvaluateOnce
        static FuncMethod EvaluateOnce(DeltinScript deltinScript) => EvaluateModifier(
            deltinScript,
            workshopFunctionName: "Evaluate Once",
            ostwFunctionName: "EvaluateOnce",
            description: "Results in the first value that was provided to the 'value' parameter for the given action or condition (useful for selectively not reevaluating certain parts of a value when used in conjunction with an action that is capable of reevaluation).",
            valueDescription: "This value is only evaluated once per action or condition."
        );

        // UpdateEveryFrame
        static FuncMethod UpdateEveryFrame(DeltinScript deltinScript) => EvaluateModifier(
            deltinScript,
            workshopFunctionName: "Update Every Frame",
            ostwFunctionName: "UpdateEveryFrame",
            description: "Results in the value that is provided to the 'value' parameter and increases the update frequency of the value to once per frame. Useful for smoothing the appearence of certain Values - such as Position Of - that normally only update every few frames. Applies to Conditions as well as Action parameters that reevaluate. May increase server load and/or lower framerates.",
            valueDescription: "This value's update rate will be increased from once every few frames to every frame."
        );

        static FuncMethod EvaluateModifier(
            DeltinScript deltinScript,
            string workshopFunctionName, // The name of the workshop function.
            string ostwFunctionName, // The name of the function as seen in OSTW.
            string description, // The description of the function.
            string valueDescription // The description of the function's value parameter.
        )
        {
            // The return type and the parameter type of the evaluation modifier.
            var parameterAndReturnType = new AnonymousType("T", AnonymousTypeContext.Function);

            // Create a new FuncMethod.
            return new FuncMethodBuilder()
            {
                Name = ostwFunctionName,
                Documentation = description,
                Parameters = new[] {
                    new CodeParameter("value", valueDescription, parameterAndReturnType)
                },
                TypeArgs = new[] { parameterAndReturnType },
                ReturnType = parameterAndReturnType,
                Action = (actionSet, methodCall) => Element.Part(workshopFunctionName, methodCall.Get(0)),
            };
        }
    }
}