using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Csv;

namespace Deltin.Deltinteger.Debugger
{
    public class DebugVariableLinkCollection
    {
        public List<IDebugVariable> Variables { get; } = new List<IDebugVariable>();
        private int _currentReference = 0;

        public void Add(IIndexReferencer referencer, IndexReference value)
        {
            int[] index = new int[value.Index.Length];
            for (int i = 0; i < index.Length; i++)
                if (value.Index[i] is V_Number number)
                    index[i] = (int)number.Value;
                else
                    return;
                
            Variables.Add(new LinkableDebugVariable(this, referencer, value.WorkshopVariable, index));
        }

        public void Add(IDebugVariable variable)
        {
            Variables.Add(variable);
        }

        public void Apply(DebuggerActionStream actionStream)
        {
            foreach (LinkableDebugVariable variable in Variables)
            {
                // Reset the obtained variable.
                variable.ResetStreamVariable();

                // Make sure the set matches.
                if (variable.Variable.IsGlobal == (actionStream.Set == DebuggerActionStreamSet.Global))
                {
                    // Get the related variable
                    foreach (var debuggerVariable in actionStream.Variables)
                        if (debuggerVariable.Name == variable.Variable.Name)
                        {
                            variable.SetStreamVariable(debuggerVariable);
                            break;
                        }
                }
            }
        }

        public int GetReference()
        {
            _currentReference++;
            return _currentReference;
        }

        public DBPVariable[] GetVariables(VariablesArgs args)
        {
            if (args.variablesReference == 0 || args.variablesReference >= 1000)
            {
                return Variables.Where(v => v.IsRoot).Select(v => v.GetProtocolVariable()).Where(v => v != null).ToArray();
            }
            else
            {
                // Child variables
                var linkVariable = Variables.FirstOrDefault(v => v.Reference == args.variablesReference);
                if (linkVariable != null)
                    return linkVariable.GetChildren(this).Select(v => v.GetProtocolVariable()).Where(v => v != null).ToArray();
            }
            return new DBPVariable[0];
        }
    }

    public interface IDebugVariable
    {
        string Name { get; }
        string Type { get; }
        int Reference { get; }
        bool IsRoot { get; }
        IDebugVariable[] GetChildren(DebugVariableLinkCollection collection);
        CsvPart GetValue();
        DBPVariable GetProtocolVariable();
    }

    public class LinkableDebugVariable : IDebugVariable
    {
        private readonly IDebugVariableResolver _resolver;
        public bool IsRoot => true;
        public string Name { get; }
        public string Type { get; }
        public WorkshopVariable Variable { get; }
        public int[] Index { get; }
        public int Reference { get; }
        public StreamVariable ActionVariable { get; private set; }
        private IDebugVariable[] _children = null;

        public LinkableDebugVariable(DebugVariableLinkCollection collection, IIndexReferencer referencer, WorkshopVariable variable, int[] index)
        {
            _resolver = referencer.Type().DebugVariableResolver ?? new DefaultResolver();
            Name = referencer.Name;
            Type = referencer.Type()?.GetName() ?? "define";
            Variable = variable;
            Index = index;
            Reference = _resolver.IsStructured() ? collection.GetReference() : 0;
        }

        public void SetStreamVariable(StreamVariable variable)
        {
            ActionVariable = variable;
        }

        public void ResetStreamVariable()
        {
            ActionVariable = null;
        }

        public DBPVariable GetProtocolVariable()
        {
            if (ActionVariable == null) return null;
            DBPVariable variable = new DBPVariable() {
                name = Name,
                type = Type,
                value = ActionVariable.Value.ToString(),
                variablesReference = Reference
            };
            _resolver.Apply(this, variable);
            return variable;
        }

        public CsvPart GetValue() => ActionVariable.Value;

        public IDebugVariable[] GetChildren(DebugVariableLinkCollection collection)
        {
            if (_children == null)
                _children = _resolver.GetChildren(collection, this);
            return _children;
        }
    }

    public class ChildDebugVariable : IDebugVariable
    {
        private readonly IDebugVariableResolver _resolver;
        public bool IsRoot => false;
        public string Name { get; }
        public string Type { get; }
        public int Reference { get; }
        public CsvPart Value { get; }
        private IDebugVariable[] _children = null;

        public ChildDebugVariable(IDebugVariableResolver resolver, CsvPart value, string name, string type, int reference)
        {
            _resolver = resolver;
            Name = name;
            Type = type;
            Reference = reference;
            Value = value;
        }

        public DBPVariable GetProtocolVariable()
        {
            if (Value == null) return null;
            DBPVariable variable = new DBPVariable() {
                name = Name,
                type = Type,
                value = Value.ToString(),
                variablesReference = Reference
            };
            _resolver.Apply(this, variable);
            return variable;
        }

        public CsvPart GetValue() => Value;

        public IDebugVariable[] GetChildren(DebugVariableLinkCollection collection)
        {
            if (_children == null)
                _children = _resolver.GetChildren(collection, this);
            return _children;
        }
    }

    public class DBPVariable
    {
        public string name;
        public string value;
        public string type;
        public int variablesReference;
        public int namedVariables;
        public int indexedVariables; 
    }

    public interface IDebugVariableResolver
    {
        bool IsStructured();
        void Apply(IDebugVariable debugVariable, DBPVariable outVariable);
        IDebugVariable[] GetChildren(DebugVariableLinkCollection collection, IDebugVariable parent);
        int GetReference(DebugVariableLinkCollection collection)
        {
            if (IsStructured()) return collection.GetReference();
            return 0;
        }
    }

    class DefaultResolver : IDebugVariableResolver
    {
        public void Apply(IDebugVariable debugVariable, DBPVariable outVariable) {}

        public bool IsStructured() => false;

        public IDebugVariable[] GetChildren(DebugVariableLinkCollection collection, IDebugVariable parent)
            => throw new NotImplementedException();
    }

    class ArrayResolver : IDebugVariableResolver
    {
        private readonly IDebugVariableResolver _typeResolver;
        private readonly string _arrayOfTypeName;

        public ArrayResolver(IDebugVariableResolver typeResolver, string arrayOfTypeName)
        {
            _typeResolver = typeResolver ?? new DefaultResolver();
            _arrayOfTypeName = arrayOfTypeName ?? "define";
        }

        public void Apply(IDebugVariable debugVariable, DBPVariable outVariable)
        {
            if (debugVariable.GetValue() is CsvArray array)
                outVariable.indexedVariables = array.Values.Length;
        }

        public IDebugVariable[] GetChildren(DebugVariableLinkCollection collection, IDebugVariable parent)
        {
            // Get the array.
            CsvArray array = parent.GetValue() as CsvArray;

            // No children if the value is not an array.
            if (array == null) return new LinkableDebugVariable[0];

            // Get the values.
            IDebugVariable[] children = new IDebugVariable[array.Values.Length];
            for (int i = 0; i < children.Length; i++)
            {
                children[i] = new ChildDebugVariable(_typeResolver, array.Values[i], "[" + i + "]", _arrayOfTypeName, _typeResolver.GetReference(collection));
                collection.Add(children[i]);
            }
            
            // Done
            return children;
        }

        public bool IsStructured() => true;
    }
}