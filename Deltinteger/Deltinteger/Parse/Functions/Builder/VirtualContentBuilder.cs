using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    class VirtualContentBuilder
    {
        readonly ActionSet _actionSet;
        readonly IEnumerable<IVirtualFunctionHandler> _functions;
        readonly ClassWorkshopInitializerComponent _classInitializer;

        public VirtualContentBuilder(ActionSet actionSet, IEnumerable<IVirtualFunctionHandler> functions)
        {
            _actionSet = actionSet;
            _functions = functions;
            _classInitializer = _actionSet.ToWorkshop.GetComponent<ClassWorkshopInitializerComponent>();
        }

        public void Build()
        {
            // Only parse the first option if there is only one.
            if (_functions.Count() == 1)
            {
                _functions.First().Build(_actionSet);
                return;
            }

            // Create the switch that chooses the overload.
            SwitchBuilder typeSwitch = new SwitchBuilder(_actionSet);

            // All types that the functions are inside.
            var allContainingTypes = from func in _functions select func.ContainingType().Provider;

            foreach (IVirtualFunctionHandler option in _functions)
            {
                // The action set for the overload.
                ActionSet optionSet = _actionSet.New(_actionSet.IndexAssigner.CreateContained());

                var containingType = option.ContainingType();

                // Add the object variables of the selected method.
                containingType.AddObjectVariablesToAssigner(optionSet.ToWorkshop, optionSet.CurrentObject, optionSet.IndexAssigner);

                // Go to next case then parse the block.
                typeSwitch.NextCase(Element.Num(_classInitializer.GetIdentifier(containingType.Provider)));

                // Iterate through every type that extends the current function's class.
                foreach (IClassInitializer type in _actionSet.ToWorkshop.Relations.GetAllExtendersOf(containingType))
                    // If 'type' does not equal the current virtual option's containing class...
                    if (containingType.Provider != type
                        // ...and 'type' does not have their own function implementation...
                        && AutoImplemented(containingType.Provider, allContainingTypes, type))
                        // ...then add an additional case for 'type's class identifier.
                        typeSwitch.NextCase(Element.Num(_classInitializer.GetIdentifier(type)));
                
                option.Build(optionSet);
            }

            ClassData classData = _actionSet.Translate.DeltinScript.GetComponent<ClassData>();

            // Finish the switch.
            typeSwitch.Finish(Element.ValueInArray(classData.ClassIndexes.GetVariable(), _actionSet.CurrentObject));
        }

        /// <summary>Determines if the specified type does not have their own implementation for the specified virtual function.</summary>
        /// <param name="virtualFunction">The virtual function to check overrides of.</param>
        /// <param name="options">All potential virtual functions.</param>
        /// <param name="type">The type to check.</param>
        public static bool AutoImplemented(IClassInitializer virtualType, IEnumerable<IClassInitializer> allOptionTypes, IClassInitializer type)
        {
            // Go through each class in the inheritance tree and check if it implements the function.
            IClassInitializer current = type;
            while (current != null && current != virtualType)
            {
                // If it does, return false.
                if (allOptionTypes.Contains(current)) return false;
                current = current.Extends.Provider;
            }
            return true;
        }
    }

    interface IVirtualFunctionHandler
    {
        ClassType ContainingType();
        void Build(ActionSet actionSet);
    }
}