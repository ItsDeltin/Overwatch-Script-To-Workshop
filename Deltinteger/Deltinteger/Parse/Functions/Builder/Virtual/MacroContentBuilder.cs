using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder.Virtual
{
    class MacroContentBuilder : AbstractVirtualContentBuilder<IMacroOption>
    {
        // The final workshop value.
        public IWorkshopTree Value { get; private set; }

        // The list of the virtual options as workshop values.
        readonly List<IWorkshopTree> _virtualValues = new List<IWorkshopTree>();

        // Links class identifiers to a virtual value.
        readonly List<VirtualValueMap> _valueMaps = new List<VirtualValueMap>();

        // The current virtual value being operated on. The same as doing '_virtualValues.Count - 1' where relevent.
        int _currentIndex;

        bool _mappingArrayCanBeOptimizedOut = true;

        public MacroContentBuilder(ActionSet actionSet, IEnumerable<IMacroOption> macros) : base(actionSet, macros)
        {
        }

        // There is only one macro, set Value to the only macro's value.
        protected override void OnlyOne() => Value = Functions.First().GetValue(ActionSet);

        // Nothing additional is required to instantiate the selector.
        protected override void InitiateSelector() {}

        // Prepares a new virtual value.
        protected override void InitiateNewOption(ActionSet optionSet, int classIdentifier)
        {
            _currentIndex = _virtualValues.Count; // Update _currentIndex
            _virtualValues.Add(Current.GetValue(optionSet)); // Get the macro as a workshop value and add it to the list.
            LinkClassToValue(classIdentifier); // Map the classIdentifier.
        }

        protected override void AddToCurrentOption(int classIdentifier)
        {
            LinkClassToValue(classIdentifier);
            _mappingArrayCanBeOptimizedOut = false; // If additional classes are linked to the current macro, then the mapping array can't be optimized out.
        }

        // Maps a class identifier to the current virtual macro.
        private void LinkClassToValue(int classIdentifier) => _valueMaps.Add(new VirtualValueMap(_currentIndex, classIdentifier));

        // Nothing additional is required after finishing a macro.
        protected override void FinalizeCurrentOption(ActionSet optionSet) {}

        protected override void Completed()
        {
            // The array of macro values, collected from the potential virtual options.
            var expArray = Element.CreateArray(_virtualValues.ToArray());

            // The array of class identifiers.
            var identifierArray = Element.CreateArray(_valueMaps.Select(i => Element.Num(i.Identifier)).ToArray());

            ClassData classData = ActionSet.Translate.DeltinScript.GetComponent<ClassData>();

            // The class identifier, extracted from the Action Set's current object.
            Element classIdentifier = classData.ClassIndexes.Get()[ActionSet.CurrentObject];

            if (!_mappingArrayCanBeOptimizedOut)
            {
                // Maps class identifiers (identifierArray) to a macro value (expArray).
                var mapArray = Element.CreateArray(_valueMaps.Select(i => Element.Num(i.ExpressionIndex)).ToArray());

                Value = expArray[mapArray[Element.IndexOfArrayValue(identifierArray, classIdentifier)]];
            }
            else
                // Mapping the class identifier is not required, use it directly.
                Value = expArray[Element.IndexOfArrayValue(identifierArray, classIdentifier)];
        }

        // Links a class identifier to an expression in the virtual array.
        struct VirtualValueMap
        {
            public int ExpressionIndex;
            public int Identifier;

            public VirtualValueMap(int expressionIndex, int identifier)
            {
                ExpressionIndex = expressionIndex;
                Identifier = identifier;
            }
        }
    }

    interface IMacroOption : IVirtualOptionHandler
    {
        IWorkshopTree GetValue(ActionSet actionSet);
    }
}