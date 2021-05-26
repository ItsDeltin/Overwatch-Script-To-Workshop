using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
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
        public IWorkshopTree Get(ActionSet actionSet) => GetGettable(actionSet).GetVariable();

        public void Set(ActionSet actionSet, Element reference, Element value) => GetGettable(actionSet, reference).Set(actionSet, value);

        IGettable GetGettable(ActionSet actionSet, IWorkshopTree reference = null)
        {
            reference = reference ?? actionSet.CurrentObject;

            // Get the gettable from the combo related to _classType.
            var gettables = actionSet.ToWorkshop.ClassInitializer
                .ComboFromClassType(_classType)
                .GetVariableGettables(_classType.Variables, reference);
            
            int index = Array.IndexOf(_classType.Variables, Variable);
            return gettables[index];
        }
    }
}