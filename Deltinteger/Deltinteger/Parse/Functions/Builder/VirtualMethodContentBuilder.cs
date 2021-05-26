using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    class MethodContentBuilder : AbstractVirtualContentBuilder<IVirtualMethodHandler>
    {
        SwitchBuilder _selector;

        public MethodContentBuilder(ActionSet actionSet, IEnumerable<IVirtualMethodHandler> functions) : base(actionSet, functions)
        {
        }

        protected override void OnlyOne() => Functions.First().Build(ActionSet);

        // Create the switch that chooses the overload.
        protected override void InitiateSelector() => _selector = new SwitchBuilder(ActionSet);

        // Adds a new case for the current option.
        protected override void AddToCurrentOption(int classIdentifier) => _selector.NextCase(Element.Num(classIdentifier));
        protected override void InitiateNewOption(ActionSet optionSet, int classIdentifier) => AddToCurrentOption(classIdentifier);

        // Build the function of the current option.
        protected override void FinalizeCurrentOption(ActionSet optionSet) => Current.Build(optionSet);

        // Finalize the switch.
        protected override void Completed()
        {
            ClassData classData = ActionSet.Translate.DeltinScript.GetComponent<ClassData>();
            _selector.Finish(Element.ValueInArray(classData.ClassIndexes.GetVariable(), ActionSet.CurrentObject));
        }
    }

    interface IVirtualMethodHandler : IVirtualOptionHandler
    {
        void Build(ActionSet actionSet);
    }
}