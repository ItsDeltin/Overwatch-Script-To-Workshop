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
        public List<LinkableDebugVariable> Variables { get; } = new List<LinkableDebugVariable>();
        private int _currentReference = 0;

        public void Add(IIndexReferencer referencer, IndexReference value)
        {
            int[] index = new int[value.Index.Length];
            for (int i = 0; i < index.Length; i++)
                if (value.Index[i] is V_Number number)
                    index[i] = (int)number.Value;
                else
                    return;
                
            var resolver = referencer.Type()?.DebugVariableResolver ?? new DefaultResolver();
            int reference = resolver.IsStructured() ? GetReference() : 0;
            // int reference = GetReference();

            Variables.Add(new LinkableDebugVariable(resolver, referencer.Name, referencer.Type()?.GetName() ?? "define", value.WorkshopVariable, index, reference));
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

        private int GetReference()
        {
            _currentReference++;
            return _currentReference;
        }

        public DBPVariable[] GetVariables(VariablesArgs args)
        {
            if (args.variablesReference != 0)
            {
                // TODO: optimize this guy
                return Variables.Select(v => v.GetProtocolVariable()).Where(v => v != null).ToArray();
            }
            else
            {
                var linkVariable = Variables.FirstOrDefault(v => v.Reference == args.variablesReference);
                if (linkVariable != null)
                {
                    // TODO
                    
                }
            }
            return new DBPVariable[0];
        }
    }

    public class LinkableDebugVariable
    {
        private readonly IDebugVariableResolver _resolver;
        public string Name { get; }
        public string Type { get; }
        public WorkshopVariable Variable { get; }
        public int[] Index { get; }
        public int Reference { get; }
        public StreamVariable ActionVariable { get; private set; }

        public LinkableDebugVariable(IDebugVariableResolver resolver, string name, string type, WorkshopVariable variable, int[] index, int reference)
        {
            _resolver = resolver;
            Name = name;
            Type = type;
            Variable = variable;
            Index = index;
            Reference = reference;
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
        void Apply(LinkableDebugVariable debugVariable, DBPVariable outVariable);
    }

    class DefaultResolver : IDebugVariableResolver
    {
        private readonly bool _structured;
        public int ChildVariables { get; set; }

        public DefaultResolver(bool structured = false)
        {
            _structured = structured;
        }
        public DefaultResolver(int childVariables)
        {
            ChildVariables = childVariables;
        }

        public void Apply(LinkableDebugVariable debugVariable, DBPVariable outVariable)
        {
            // Array length
            if (debugVariable.ActionVariable.Value is CsvArray array)
                outVariable.indexedVariables = array.Values.Length;
            // Child length
            else if (ChildVariables != -1)
                outVariable.namedVariables = ChildVariables;
        }
        public bool IsStructured() => _structured || ChildVariables > 0;
    }
}