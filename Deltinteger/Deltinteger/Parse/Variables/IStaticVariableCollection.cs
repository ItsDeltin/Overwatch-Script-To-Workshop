using System.Linq;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    class StaticVariableCollection : IComponent
    {
        public IEnumerable<IVariableInstance> StaticVariables => _staticVariables;
        readonly List<IVariableInstance> _staticVariables = new List<IVariableInstance>();
        public void AddVariable(IVariableInstance variableInstance) => _staticVariables.Add(variableInstance);
        public void Init(DeltinScript deltinScript) {}
    }
}