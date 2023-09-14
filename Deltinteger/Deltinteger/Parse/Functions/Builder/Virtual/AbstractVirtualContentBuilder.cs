using System.Linq;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse.Functions.Builder.Virtual
{
    /// <summary>
    /// The AbstractVirtualContentBuilder is for generating workshop output that follows the virtual class selector pattern.
    /// </summary>
    abstract class AbstractVirtualContentBuilder<T> where T: IVirtualOptionHandler
    {
        protected ActionSet ActionSet { get; }
        protected IEnumerable<T> Functions { get; }
        protected T Current { get; private set; }

        protected AbstractVirtualContentBuilder(ActionSet actionSet, IEnumerable<T> functions)
        {
            ActionSet = actionSet;
            Functions = functions;
            Build();
        }

        void Build()
        {
            // When there is only one available function.
            if (Functions.Count() == 1)
            {
                OnlyOne();
                return;
            }

            // Initiate the selector required for multiple options.
            InitiateSelector();

            // All types that the functions are inside.
            var allContainingTypes = from func in Functions select func.ContainingType();

            foreach (T option in Functions)
            {
                // The action set for the overload.
                var optionSet = ActionSet.ContainVariableAssigner();
                var containingType = option.ContainingType();

                // Add the object variables of the selected method.
                containingType.AddObjectVariablesToAssigner(optionSet.ToWorkshop, new(optionSet.CurrentObject), optionSet.IndexAssigner);

                // Go to next case then parse the block.
                var relation = ActionSet.ToWorkshop.ClassInitializer.RelationFromClassType(containingType);
                
                Current = option;
                InitiateNewOption(optionSet, relation.Combo.ID);

                // Iterate through every type that extends the current function's class.
                foreach (var type in relation.GetAllExtenders())
                    // If 'type' does not equal the current virtual option's containing class...
                    if (!containingType.Is(type.Instance)
                        // ...and 'type' does not have their own function implementation...
                        && AutoImplemented(containingType, allContainingTypes, type.Instance))
                    {
                        // ...then add an additional case for 'type's class identifier.
                        AddToCurrentOption(type.Combo.ID);
                    }
                
                FinalizeCurrentOption(optionSet);
            }

            Completed();
        }

        /// <summary>Executed when there is only one available virtual option.</summary>
        protected abstract void OnlyOne();

        /// <summary>Readies the selector used to choose a virtual option based off of the class's identifier.</summary>
        protected abstract void InitiateSelector();

        /// <summary>Instantiates the next option with the provided class identifier. The new option can be accessed via the 'Current' property.</summary>
        protected abstract void InitiateNewOption(ActionSet optionSet, int classIdentifier);

        /// <summary>Adds a class identifier to the current option.</summary>
        protected abstract void AddToCurrentOption(int classIdentifier);

        /// <summary>Executed once all class identifiers were collected for the current option.</summary>
        protected abstract void FinalizeCurrentOption(ActionSet optionSet);

        /// <summary>Executed when the abstract builder has completed. Implementers can add their own code to finalize their work.</summary>
        protected abstract void Completed();

        //   class A { f(); }
        //   class B : A { override f(); }
        //   class C : B {}
        //
        // This will make sure when C.F is called, B.F will be chosen.
        // Not checking for this will result in undefined behaviour, likely A.F will be chosen.
        static bool AutoImplemented(ClassType virtualType, IEnumerable<ClassType> allOptionTypes, ClassType type)
        {
            // Go through each class in the inheritance tree and check if it implements the function...
            while (type != null && !type.Is(virtualType))
            {
                // ...if it does, return false.
                if (allOptionTypes.Any(t => type.Is(t))) return false;
                type = (ClassType)type.Extends;
            }
            return true;
        }
    }

    interface IVirtualOptionHandler
    {
        ClassType ContainingType();
    }
}