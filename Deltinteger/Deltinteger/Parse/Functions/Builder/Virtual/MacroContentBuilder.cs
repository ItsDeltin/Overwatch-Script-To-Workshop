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

        bool _isStruct = false;

        public MacroContentBuilder(ActionSet actionSet, IEnumerable<IMacroOption> macros) : base(actionSet, macros)
        {
        }

        // There is only one macro, set Value to the only macro's value.
        protected override void OnlyOne() => Value = Functions.First().GetValue(ActionSet);

        // Nothing additional is required to instantiate the selector.
        protected override void InitiateSelector() { }

        // Prepares a new virtual value.
        protected override void InitiateNewOption(ActionSet optionSet, int classIdentifier)
        {
            _currentIndex = _virtualValues.Count; // Update _currentIndex

            var currentValue = Current.GetValue(optionSet);

            // If the macro value is a struct, we will need to bridge it.
            if (_currentIndex == 0 && currentValue is IStructValue)
                _isStruct = true;

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
        protected override void FinalizeCurrentOption(ActionSet optionSet) { }

        protected override void Completed()
        {
            if (_isStruct)
            {
                // Use the first value as the template.
                var template = (IStructValue)_virtualValues[0];

                Value = template.Bridge(bridgeArgs =>
                {
                    // Get the workshop values.
                    var steppedValues = _virtualValues
                        // Do not need to calculate GetValueWithPath, already known via bridgeArgs.Value
                        .Skip(1)
                        // Step into the overrider values with the path provided by bridgeArgs.
                        .Select(value => IStructValue.GetValueWithPath((IStructValue)value, bridgeArgs.Path))
                        // Prepend the template value.
                        .Prepend(bridgeArgs.Value);

                    return CreateVirtualMap(steppedValues);
                });
            }
            else
                Value = CreateVirtualMap(_virtualValues);
        }

        IWorkshopTree CreateVirtualMap(IEnumerable<IWorkshopTree> macroValues)
        {
            // The array of macro values, collected from the potential virtual options.
            var expArray = Element.CreateArray(macroValues.ToArray());

            // The array of class identifiers.
            var identifierArray = Element.CreateArray(_valueMaps.Select(i => Element.Num(i.Identifier)).ToArray());

            ClassData classData = ActionSet.Translate.DeltinScript.GetComponent<ClassData>();

            // The class identifier, extracted from the Action Set's current object.
            Element classIdentifier = classData.ClassIndexes.Get()[classData.GetPointer((Element)ActionSet.CurrentObject)];

            if (!_mappingArrayCanBeOptimizedOut)
            {
                // Maps class identifiers (identifierArray) to a macro value (expArray).
                var mapArray = Element.CreateArray(_valueMaps.Select(i => Element.Num(i.ExpressionIndex)).ToArray());

                return expArray[mapArray[Element.IndexOfArrayValue(identifierArray, classIdentifier)]];
            }
            else
                // Mapping the class identifier is not required, use it directly.
                return expArray[Element.IndexOfArrayValue(identifierArray, classIdentifier)];
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