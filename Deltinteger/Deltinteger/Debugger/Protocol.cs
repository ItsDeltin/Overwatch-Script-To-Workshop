namespace Deltin.Deltinteger.Debugger.Protocol
{
    public class EvaluateArgs
    {
        /// <summary>The expression to evaluate.</summary>
        public string expression;
        /// <summary>Evaluate the expression in the scope of this stack frame. If not specified, the expression is evaluated in the global scope.</summary>
        public int frameId;
        /// <summary>
        /// The context in which the evaluate request is run.
        /// Values:
        /// 'watch': evaluate is run in a watch.
        /// 'repl': evaluate is run from REPL console.
        /// 'hover': evaluate is run from a data hover.
        /// 'clipboard': evaluate is run to generate the value that will be stored in the clipboard.
        /// The attribute is only honored by a debug adapter if the capability 'supportsClipboardContext' is true.
        /// </summary>
        public string context;
    }

    public class EvaluateResponse
    {
        /// <summary>The result of the evaluate request.</summary>
        public string result;
        /// <summary>The optional type of the evaluate result.</summary>
        public string type;
        /// <summary>If variablesReference is > 0, the evaluate result is structured and its children can be retrieved by passing variablesReference to the VariablesRequest.</summary>
        public int variablesReference;
        /// <summary>
        /// The number of named child variables.
        /// The client can use this optional information to present the variables in a paged UI and fetch them in chunks.
        /// The value should be less than or equal to 2147483647 (2^31 - 1).
        /// </summary>
        public int namedVariables;
        /// <summary>
        /// The number of indexed child variables.
        /// The client can use this optional information to present the variables in a paged UI and fetch them in chunks.
        /// The value should be less than or equal to 2147483647 (2^31 - 1).
        /// </summary>
        public int indexedVariables;
    }

    public class VariablesArgs
    {
        /// <summary>The Variable reference.</summary>
        public int variablesReference;
        /// <summary>Optional filter to limit the child variables to either named or indexed. If omitted, both types are fetched.</summary>
        public string filter;
        /// <summary>The index of the first variable to return; if omitted children start at 0.</summary>
        public int start;
        /// <summary>The number of variables to return. If count is missing or 0, all variables are returned.</summary>
        public int count;
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
}