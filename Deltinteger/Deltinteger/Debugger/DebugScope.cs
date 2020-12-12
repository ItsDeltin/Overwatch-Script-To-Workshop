using System.Collections.Generic;
using Deltin.Deltinteger.Debugger.Protocol;

namespace Deltin.Deltinteger.Debugger
{
    public class DebuggerScope : IDebuggerReference
    {
        public string Name { get; }
        public List<IDebugVariable> Variables { get; } = new List<IDebugVariable>();

        public DebuggerScope(string name)
        {
            Name = name;
        }

        public DBPScope GetScope(DebugVariableLinkCollection collection) => new DBPScope()
        {
            name = Name,
            variablesReference = collection.References[this]
        };

        public IDebugVariable[] GetChildren(DebugVariableLinkCollection collection) => Variables.ToArray();
    }
}