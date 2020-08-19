using Deltin.Deltinteger.Csv;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Debugger
{
    public interface IDebugVariable
    {
        string Name { get; }
        string Type { get; }
        int Reference { get; set; }
        bool IsRoot { get; }
        CsvPart Value { get; }
        IDebugVariableResolver Resolver { get; }
        public static int ApplyReference(DebugVariableLinkCollection collection, IDebugVariable debugVariable)
        {
            if (debugVariable.Reference == 0)
                debugVariable.Reference = collection.GetReference();
            return debugVariable.Reference;
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
        public int Reference { get; set; }
        public CsvPart Value { get; private set; }

        public LinkableDebugVariable(DebugVariableLinkCollection collection, IIndexReferencer referencer, WorkshopVariable variable, int[] index)
        {
            Resolver = referencer.Type()?.DebugVariableResolver ?? new DefaultResolver();
            Name = referencer.Name;
            Type = referencer.Type()?.GetName() ?? "define";
            Variable = variable;
            Index = index;
        }

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
    }

    public class ChildDebugVariable : IDebugVariable
    {
        public bool IsRoot => false;
        public IDebugVariableResolver Resolver { get; }
        public string Name { get; }
        public string Type { get; }
        public int Reference { get; set; }
        public CsvPart Value { get; }

        public ChildDebugVariable(IDebugVariableResolver resolver, CsvPart value, string name, string type)
        {
            Resolver = resolver;
            Name = name;
            Type = type;
            Value = value;
        }
    }
}