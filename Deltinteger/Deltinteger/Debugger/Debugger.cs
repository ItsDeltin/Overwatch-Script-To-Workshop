using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Debugger.Protocol;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Csv;
using Deltin.Deltinteger.Decompiler.TextToElement;
using TextCopy;

namespace Deltin.Deltinteger.Debugger
{
    class ClipboardListener
    {
        public DebugVariableLinkCollection VariableCollection;
        private readonly DeltintegerLanguageServer _languageServer;
        private readonly object _currentLock = new object();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public ClipboardListener(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
        }

        public void Start()
        {
            lock (_currentLock)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                Task.Run(async () => await Listen(), _cancellationTokenSource.Token);
            }
        }

        async Task Listen()
        {
            var token = _cancellationTokenSource.Token;

            string last = null;
            while (!token.IsCancellationRequested && !token.WaitHandle.WaitOne(500))
            {
                string clipboard = Clipboard.GetText(); // Get clipboard.
                if (clipboard == last || clipboard == null) continue; // Clipboard did not change.
                last = clipboard;

                // Clipboard changed.

                try
                {
                    // As workshop actions
                    ConvertTextToElement tte = new ConvertTextToElement(clipboard);
                    Workshop workshop = tte.GetActionList();

                    if (workshop != null) // Determines if the clipboard is an action list.
                    {
                        DebuggerActionSetResult actionStream = new DebuggerActionSetResult(workshop);

                        // Action list successfully parsed.
                        // Get the DeltinScript.
                        VariableCollection = (await _languageServer.DocumentHandler.OnScriptAvailability())?.DebugVariables;

                        // Error obtaining debug variables.
                        if (VariableCollection == null) return;

                        // Apply debugger variables.
                        VariableCollection.Apply(actionStream);

                        // Notify the adapter of the new state.
                        _languageServer.Server.SendNotification("debugger.activated");
                    }
                }
                catch (Exception ex)
                {
                    _languageServer.DebuggerException(ex);
                }
            }
        }

        public void Stop()
        {
            lock (_currentLock)
                _cancellationTokenSource.Cancel();
        }
    }

    public class DebuggerActionSetResult
    {
        public DebuggerActionStreamSet Set { get; }
        public StreamVariable[] Variables { get; }

        public DebuggerActionSetResult(Workshop workshop)
        {
            // Get the variables.
            Variables = new StreamVariable[workshop.Variables.Length];
            for (int i = 0; i < Variables.Length; i++)
                Variables[i] = new StreamVariable(workshop.Variables[i].ID, workshop.Variables[i].Name);

            // Get the variable values.
            foreach (var action in workshop.Actions)
                if (action is SetVariableAction setVariable)
                {
                    // Set the variable kind.
                    if (Set == DebuggerActionStreamSet.Unknown)
                    {
                        // The variable is a global variable.
                        if (setVariable.Variable is GlobalVariableExpression)
                            Set = DebuggerActionStreamSet.Global;
                        // The varaible is a player variable.
                        else
                            Set = DebuggerActionStreamSet.Player;
                    }

                    // Error if the operator is incorrect.
                    if (setVariable.Operator != "=") Error($"Variable is not being set with '{setVariable.Operator}' rather than '='.");
                    // Error if set variable's index is not null.
                    if (setVariable.Index != null) Error("Variable is incorrectly being set at an index.");

                    // Get the variable.
                    string variableName = setVariable.Variable.Name;
                    StreamVariable relatedVariable = Array.Find(Variables, value => value.Name == variableName);

                    // Get the value.
                    var value = PartFromExpression(setVariable.Value);
                    relatedVariable.Value = value;
                }
                else
                    Error("Action is not setting a variable.");
        }

        private void Error(string msg)
        {
            throw new Exception(msg);
        }

        private CsvPart PartFromExpression(ITTEExpression expression)
        {
            if (expression is NumberExpression num) return new CsvNumber(num.Value);
            else if (expression is StringExpression str) return new CsvString(str.Value);
            // Others
            else if (expression is FunctionExpression func)
            {
                switch (func.Function.Name)
                {
                    // Array
                    case "Array": return new CsvArray(func.Values.Select(v => PartFromExpression(v)).ToArray());
                    // True
                    case "True": return new CsvBoolean(true);
                    // False
                    case "False": return new CsvBoolean(false);
                    // Null
                    case "Null": return new CsvNull();
                    // Vector
                    case "Vector":
                        return new CsvVector(new Models.Vertex(
             ExtractComponent(func, 0, "X"),
             ExtractComponent(func, 1, "Y"),
             ExtractComponent(func, 2, "Z")
         ));
                    // Default
                    default:
                        Error("Unsure of how to handle function '" + func.Function.Name);
                        return null;
                }
            }

            Error("Unsure of how to handle expression of type '" + expression.GetType().Name + "'.");
            return null;
        }

        private double ExtractComponent(FunctionExpression function, int index, string name)
        {
            // Not enough values.
            if (function.Values.Length <= index)
                Error("Could not get the '" + name + "' component, there are only " + function.Values.Length + " values.");
            else
            {
                // If the value is a number, return the number.
                if (function.Values[index] is NumberExpression num)
                    return num.Value;
                // Error if the value is not a number.
                else
                    Error("Could not get the '" + name + "' component, the value is a '" + function.Values[index].GetType().Name + "' rather than a number.");
            }
            // Default
            return 0;
        }
    }

    public class StreamVariable
    {
        public int Index { get; }
        public string Name { get; }
        public CsvPart Value { get; set; }

        public StreamVariable(int index, string name)
        {
            Index = index;
            Name = name;
        }
    }

    public enum DebuggerActionStreamSet
    {
        Unknown,
        Global,
        Player
    }
}