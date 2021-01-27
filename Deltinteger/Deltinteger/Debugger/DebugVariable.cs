using System.Collections.Generic;
using Deltin.Deltinteger.Debugger.Protocol;
using Deltin.Deltinteger.Csv;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Debugger
{
    public interface IDebugVariable : IDebuggerReference
    {
        string Name { get; }
        string Type { get; }
        bool IsRoot { get; }
        CsvPart Value { get; }
        IDebugVariableResolver Resolver { get; }
        public static int ApplyReference(DebugVariableLinkCollection collection, IDebugVariable debugVariable)
        {
            if (!collection.References.ContainsKey(debugVariable))
                collection.References.Add(debugVariable, collection.GetReference());

            return collection.References[debugVariable];
        }
    }

    public class LinkableDebugVariable : IDebugVariable
    {
        public bool IsRoot => true;
        public IDebugVariableResolver Resolver { get; }
        public string Name { get; }
        public string Type { get; }
        public WorkshopVariable Variable { get; }
        public int[] Index { get; }
        public CsvPart Value { get; private set; }

        public LinkableDebugVariable(DebugVariableLinkCollection collection, IVariable scriptVariable, WorkshopVariable workshopVariable, int[] index)
        {
            // todo
            // Resolver = scriptVariable.CodeType.DebugVariableResolver ?? new DefaultResolver();
            Name = scriptVariable.Name;
            // Type = scriptVariable.CodeType.GetName();
            Variable = workshopVariable;
            Index = index;
        }

        public IDebugVariable[] GetChildren(DebugVariableLinkCollection collection) => Resolver.GetChildren(collection, this);

        public void SetStreamVariable(StreamVariable variable)
        {
            Value = variable.Value;
            for (int i = 0; i < Index.Length; i++)
                Value = ((CsvArray)Value).Values[Index[i]];
        }

        public void ResetStreamVariable()
        {
            Value = null;
        }

        public override string ToString() => Name + (Value == null ? "" : " = " + Value.ToString());
    }

    public class ChildDebugVariable : IDebugVariable
    {
        public bool IsRoot => false;
        public IDebugVariableResolver Resolver { get; }
        public string Name { get; }
        public string Type { get; }
        public CsvPart Value { get; }

        public ChildDebugVariable(IDebugVariableResolver resolver, CsvPart value, string name, string type)
        {
            Resolver = resolver;
            Name = name;
            Type = type;
            Value = value;
        }

        public IDebugVariable[] GetChildren(DebugVariableLinkCollection collection) => Resolver.GetChildren(collection, this);

        public override string ToString() => Name + (Value == null ? "" : " = " + Value.ToString());
    }
}