using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public class VariableModifierGroup
    {
        readonly HashSet<IVariableInstance> _unsettable = new HashSet<IVariableInstance>();
        public void MakeUnsettable(IVariableInstance variableInstance) => _unsettable.Add(variableInstance);
        public bool IsSettable(IVariableInstance variableInstance) => !_unsettable.Contains(variableInstance);
    }
}