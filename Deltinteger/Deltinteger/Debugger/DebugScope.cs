using System.Collections.Generic;
using Deltin.Deltinteger.Debugger.Protocol;

namespace Deltin.Deltinteger.Debugger
{
    public class DebuggerScope
    {
        public string Name { get; }
        public int Reference { get; }
        public List<IDebugVariable> Variables { get; } = new List<IDebugVariable>();

        public DebuggerScope(string name, int reference)
        {
            Name = name;
            Reference = reference;
        }

        public DBPScope GetScope() => new DBPScope() {
            name = Name,
            variablesReference = Reference
        };
    }
}