using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Workshop;

namespace Deltin.Deltinteger.Parse
{
    /// <summary>Provides a way to get and set instance variables.</summary>
    public class ObjectVariable
    {
        public IVariableInstance Variable { get; }
        readonly ClassType _classType;

        public ObjectVariable(ClassType classType, IVariableInstance variable)
        {
            _classType = classType;
            Variable = variable;
        }

        /// <summary>Gets the value from the current context's object reference.</summary>
        public Element Get(ActionSet actionSet) => (Element)GetGettable(actionSet).GetVariable();

        /// <summary>Gets the value from an object reference.</summary>
        public Element Get(ToWorkshop toWorkshop, IWorkshopTree reference) => (Element)GetGettable(toWorkshop, reference).GetVariable();

        /// <summary>Gets the value from an object reference.</summary>
        public void Set(ActionSet actionSet, IWorkshopTree value, params Element[] index) => SetWithReference(actionSet, actionSet.CurrentObject, value, index);

        /// <summary>Sets the value of the ObjectVariable.</summary>
        public void SetWithReference(ActionSet actionSet, IWorkshopTree reference, IWorkshopTree value, params Element[] index) => GetGettable(actionSet, reference).Set(actionSet, value, null, index);

        public void Modify(ActionSet actionSet, Operation operation, IWorkshopTree value, params Element[] index) => GetGettable(actionSet).Modify(actionSet, operation, value, null, index);

        public void Modify(ActionSet actionSet, IWorkshopTree reference, Operation operation, IWorkshopTree value, params Element[] index) =>
            GetGettable(actionSet).Modify(actionSet, operation, value, null, index);

        /// <summary>Adds the ObjectVariable to a variable assigner.</summary>
        public void AddToAssigner(VarIndexAssigner assigner, ToWorkshop toWorkshop, IWorkshopTree reference) =>
            assigner.Add(Variable.Provider, GetGettable(toWorkshop, reference));

        // Extracts the Gettable from an ActionSet. If a reference is not provided, actionSet.CurrentObject is used by default.
        IGettable GetGettable(ActionSet actionSet, IWorkshopTree reference = null) => GetGettable(actionSet.ToWorkshop, reference ?? actionSet.CurrentObject);

        // Extracts the Gettable from an object reference.
        IGettable GetGettable(ToWorkshop toWorkshop, IWorkshopTree reference)
        {
            // Get the gettable from the combo related to _classType.
            var gettables = toWorkshop.ClassInitializer
                .ComboFromClassType(_classType)
                .GetVariableGettables(_classType.Variables, reference);

            return gettables[Variable];
        }
    }
}