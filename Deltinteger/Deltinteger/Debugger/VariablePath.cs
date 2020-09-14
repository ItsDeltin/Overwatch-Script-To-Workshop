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
        /// <summary>Vraiables created in the script.</summary>
        public List<LinkableDebugVariable> LinkableVariables { get; } = new List<LinkableDebugVariable>();
        /// <summary>All variables including children.</summary>
        public List<IDebugVariable> Variables { get; private set; } = new List<IDebugVariable>();
        /// <summary>The scopes in the project.</summary>
        public List<DebuggerScope> Scopes { get; } = new List<DebuggerScope>();
        /// <summary>All assigned references, includes scopes and variables.</summary>
        public Dictionary<IDebuggerReference, int> References { get; } = new Dictionary<IDebuggerReference, int>();
        /// <summary>A copied variable's value.</summary>
        public Dictionary<string, string> ClipboardEvaluation { get; } = new Dictionary<string, string>();
        /// <summary>The copied inspector values.</summary>
        public DebuggerActionSetResult ActionStream { get; private set; }
        /// <summary>The current reference.</summary>
        private int _currentReference = 0;
        /// <summary>The variables scope, containing variables from the script.</summary>
        private readonly DebuggerScope _variablesScope = new DebuggerScope("Variables");
        /// <summary>The 'raw scope', containing variables directly from ActionStream.</summary>
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
                if (value.Index[i] is NumberElement number)
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
            foreach (var reference in References)
                if (reference.Value == args.variablesReference)
                    return reference.Key.GetChildren(this).Select(v => v.Resolver.GetVariable(this, v)).Where(v => v != null).Skip(args.start).Take(args.count == 0 ? int.MaxValue : args.count).ToArray();
            
            return new DBPVariable[0];
        }

        /// <summary>Gets the variable scopes.</summary>
        public DBPScope[] GetScopes(ScopesArgs args) => Scopes.Select(scope => scope.GetScope(this)).ToArray();

        /// <summary>Evaluates an expression.</summary>
        public EvaluateResponse Evaluate(EvaluateArgs args)
        {
            if (args.context == "clipboard")
                return new EvaluateResponse() {
                    result = ClipboardEvaluation[args.expression]
                };

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

        public string AddClipboardKey(string original, string value)
        {
            original = "!clipboard." + original;
            string current = original;

            for (int i = 0; ClipboardEvaluation.ContainsKey(current); i++)
                current = original + "_" + i;
            
            ClipboardEvaluation.Add(current, value);
            return current;
        }
    }

    public interface IDebuggerReference
    {
        IDebugVariable[] GetChildren(DebugVariableLinkCollection collection);
    }
}