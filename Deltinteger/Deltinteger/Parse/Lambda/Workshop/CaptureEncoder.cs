using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using static Deltin.Deltinteger.Elements.Element;

namespace Deltin.Deltinteger.Parse.Lambda.Workshop
{
    static class CaptureEncoder
    {
        public static Element Encode(ActionSet actionSet, LambdaAction lambdaAction)
        {
            var meta = new List<IWorkshopTree>();

            // The first element is the lambda's identifier.
            meta.Add(Num(actionSet.ToWorkshop.LambdaBuilder.GetIdentifier(actionSet, new AnonymousWorkshopPortableFunction(lambdaAction))));

            // The second element is the 'this' if applicable.
            meta.Add(actionSet.This ?? Null());

            // Add captured variables.
            foreach (var captured in lambdaAction.CapturedVariables)
            {
                // Get the assigner from the captured variable.
                var gettable = actionSet.IndexAssigner[captured.Provider];

                // Struct
                if (gettable is IStructValue structValue)
                    foreach (var child in structValue.GetAllValues())
                        meta.Add(child);
                
                // Normal
                else
                    meta.Add(gettable.GetVariable());
            }

            return CreateArray(meta.ToArray());
        }

        public static Element Encode(ActionSet actionSet, CallMethodGroup methodGroup) => CreateArray(
            // First element
            Num(actionSet.ToWorkshop.LambdaBuilder.GetIdentifier(actionSet, new MethodGroupWorkshopPortableFunction(actionSet.DeltinScript, methodGroup))),
            // Second element
            actionSet.This ?? Null());

        public static void DecodeCaptured(ActionSet actionSet, IWorkshopTree source, LambdaAction lambdaAction)
        {
            // 0 is the lambda identifier, 1 is the local object, so start at 2 for captured variables.
            var unfolder = new LambdaUnfolder(source, 2);

            foreach (var captured in lambdaAction.CapturedVariables)
            {
                // Get the gettable from the captured variable.
                var gettable = captured.GetAssigner(new(actionSet)).Unfold(unfolder);

                // Add to the assigner.
                actionSet.IndexAssigner.Add(captured.Provider, gettable);
            }
        }

        class LambdaUnfolder : IUnfoldGettable
        {
            readonly IWorkshopTree _source;
            int _index;

            public LambdaUnfolder(IWorkshopTree source, int offset)
            {
                _source = source;
                _index = offset;
            }

            public IWorkshopTree NextValue() => ValueInArray(_source, Num(_index++));
        }
    }
}