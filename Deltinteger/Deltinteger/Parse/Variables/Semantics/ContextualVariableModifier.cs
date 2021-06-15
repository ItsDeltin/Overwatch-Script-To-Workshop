using System.Linq;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public class VariableModifierGroup
    {
        readonly HashSet<IVariableInstance> _unsettable = new HashSet<IVariableInstance>();
        public void MakeUnsettable(DeltinScript deltinScript, IVariableInstance variableInstance)
        {
            _unsettable.Add(variableInstance);
            variableInstance.CodeType.GetCodeType(deltinScript).TypeSemantics.MakeUnsettable(deltinScript, this);
        }
        public bool IsSettable(IVariableInstance variableInstance) => !_unsettable.Contains(variableInstance);
        public bool ContainsProvider(IVariable provider) => _unsettable.Any(instance => instance.Provider == provider);
    }
}