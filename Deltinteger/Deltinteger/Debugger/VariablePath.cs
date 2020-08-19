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
        public Dictionary<object, int> References { get; } = new Dictionary<object, int>();
        public List<DebuggerScope> Scopes { get; } = new List<DebuggerScope>();
        public DebuggerActionSetResult ActionStream { get; private set; }
        private int _currentReference = 0;
        private readonly DebuggerScope _variablesScope = new DebuggerScope("Variables");
        private readonly DebuggerScope _rawScope = new DebuggerScope("Raw");

        public DebugVariableLinkCollection()
        {
            Scopes.Add(_variablesScope);
            Scopes.Add(_rawScope);
        }

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
            _variablesScope.Variables.Add(newVariable);
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

            // Reset the references.
            References.Clear();
            _currentReference = 0;

            // Add scope references.
            References.Add(_variablesScope, GetReference());
            References.Add(_rawScope, GetReference());
            _rawScope.Variables.Clear();
            
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

            // Raw variables
            foreach (var value in actionStream.Variables)
            {
                var variable = new ChildDebugVariable(new DefaultResolver(), value.Value, value.Name, null);
                Add(variable);
                _rawScope.Variables.Add(variable);
            }
        }

        public int GetReference()
        {
            _currentReference++;
            return _currentReference;
        }

        /// <summary>Gets the variables.</summary>
        public DBPVariable[] GetVariables(VariablesArgs args)
        {
            foreach (var scope in Scopes)
                if (References[scope] == args.variablesReference)
                    return scope.Variables.Select(v => v.Resolver.GetVariable(this, v)).Where(v => v != null).ToArray();
            
            foreach (var variable in Variables)
                if (References.TryGetValue(variable, out int reference) && reference == args.variablesReference)
                    return variable.Resolver.GetChildren(this, variable).Select(v => v.Resolver.GetVariable(this, v)).Where(v => v != null).ToArray();

            return new DBPVariable[0];
        }

        /// <summary>Gets the variable scopes.</summary>
        public DBPScope[] GetScopes(ScopesArgs args) => Scopes.Select(scope => scope.GetScope(this)).ToArray();

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
}