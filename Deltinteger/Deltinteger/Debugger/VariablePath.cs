using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Csv;
using Deltin.Deltinteger.Debugger.Protocol;

namespace Deltin.Deltinteger.Debugger
{
    public class DebugVariableLinkCollection
    {
        public List<LinkableDebugVariable> LinkableVariables { get; } = new List<LinkableDebugVariable>();
        public List<IDebugVariable> Variables { get; private set; } = new List<IDebugVariable>();
        public DebuggerActionSetResult ActionStream { get; private set; }
        private int _currentReference = 0;

        public void Add(IIndexReferencer referencer, IndexReference value)
        {
            int[] index = new int[value.Index.Length];
            for (int i = 0; i < index.Length; i++)
                if (value.Index[i] is V_Number number)
                    index[i] = (int)number.Value;
                else
                    return;
                
            var newVariable = new LinkableDebugVariable(this, referencer, value.WorkshopVariable, index);
            Variables.Add(newVariable);
            LinkableVariables.Add(newVariable);
        }

        public void Add(IDebugVariable variable)
        {
            Variables.Add(variable);
        }

        public void Apply(DebuggerActionSetResult actionStream)
        {
            ActionStream = actionStream;

            // Reset the variables list.
            Variables = new List<IDebugVariable>(LinkableVariables);
            
            foreach (LinkableDebugVariable variable in LinkableVariables)
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
                return Variables.Where(v => v.IsRoot).Select(v => v.Resolver.GetVariable(this, v)).Where(v => v != null).ToArray();
            }
            else
            {
                // Child variables
                var linkVariable = Variables.FirstOrDefault(v => v.Reference == args.variablesReference);
                if (linkVariable != null)
                    return linkVariable.Resolver.GetChildren(this, linkVariable).Select(v => v.Resolver.GetVariable(this, v)).Where(v => v != null).ToArray();
            }
            return new DBPVariable[0];
        }

        public EvaluateResponse Evaluate(EvaluateArgs args)
        {
            string[] path = args.expression.Split('.');

            // Get the first variable.
            IDebugVariable current = Variables.FirstOrDefault(v => v.IsRoot && v.Name == path[0]);
            if (current == null) return null;

            // Get the rest of the path.
            for (int i = 1; i < path.Length; i++)
            {
                current = current.Resolver.GetChildren(this, current).FirstOrDefault(v => v.Name == path[i]);
                if (current == null) return null;
            }

            return current.Resolver.GetEvaluation(this, current);
        }
    }

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

    public interface IDebugVariableResolver
    {
        DBPVariable GetVariable(DebugVariableLinkCollection collection, IDebugVariable debugVariable);
        EvaluateResponse GetEvaluation(DebugVariableLinkCollection collection, IDebugVariable debugVariable);
        IDebugVariable[] GetChildren(DebugVariableLinkCollection collection, IDebugVariable parent);
    }

    class DefaultResolver : IDebugVariableResolver
    {
        public DBPVariable GetVariable(DebugVariableLinkCollection collection, IDebugVariable debugVariable) {
            // Return null if there is no value.
            if (debugVariable.Value == null) return null;

            // Create the variable.
            DBPVariable variable = new DBPVariable(debugVariable);

            if (debugVariable.Value is CsvArray array)
            {
                variable.indexedVariables = array.Values.Length;
                variable.variablesReference = IDebugVariable.ApplyReference(collection, debugVariable);
            }

            return variable;
        }
        public EvaluateResponse GetEvaluation(DebugVariableLinkCollection collection, IDebugVariable debugVariable) {
            // Return null if there is no value.
            if (debugVariable.Value == null) return null;

            // Create the evaluation response.
            EvaluateResponse response = new EvaluateResponse(debugVariable);

            if (debugVariable.Value is CsvArray array)
            {
                response.indexedVariables = array.Values.Length;
                response.variablesReference = IDebugVariable.ApplyReference(collection, debugVariable);
            }
            
            return response;
        }

        public virtual IDebugVariable[] GetChildren(DebugVariableLinkCollection collection, IDebugVariable parent)
        {
            // Get the array.
            CsvArray array = parent.Value as CsvArray;

            // No children if the value is not an array.
            if (array == null) return new LinkableDebugVariable[0];

            // Get the values.
            IDebugVariable[] children = new IDebugVariable[array.Values.Length];
            for (int i = 0; i < children.Length; i++)
            {
                children[i] = GetChildDebugVariable(array.Values[i], "[" + i + "]");
                collection.Add(children[i]);
            }
            
            // Done
            return children;
        }

        protected virtual ChildDebugVariable GetChildDebugVariable(CsvPart arrayValue, string indexName) => new ChildDebugVariable(this, arrayValue, indexName, "define");
    }

    class ArrayResolver : DefaultResolver
    {
        private readonly IDebugVariableResolver _typeResolver;
        private readonly string _arrayOfTypeName;

        public ArrayResolver(IDebugVariableResolver typeResolver, string arrayOfTypeName)
        {
            _typeResolver = typeResolver ?? new DefaultResolver();
            _arrayOfTypeName = arrayOfTypeName ?? "define";
        }

        protected override ChildDebugVariable GetChildDebugVariable(CsvPart arrayValue, string indexName) => new ChildDebugVariable(_typeResolver, arrayValue, indexName, _arrayOfTypeName);
    }
}