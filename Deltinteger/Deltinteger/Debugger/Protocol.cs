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

        public EvaluateResponse() {}
        public EvaluateResponse(DebugVariableLinkCollection collection, IDebugVariable variable)
        {
            type = variable.Type;
            result = variable.Value.ToString();
            variablesReference = collection.References[variable];
        }
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

    public class ScopesArgs
    {
        /// <summary>Retrieve the scopes for this stackframe.</summary>
        public int frameId;
    }

    /// <summary>
    /// A Scope is a named container for variables. Optionally a scope can map to a source or a range within a source.
    /// </summary>
    public class DBPScope
    {
        /// <summary>
        /// Name of the scope such as 'Arguments', 'Locals', or 'Registers'. This string is shown in the UI as is and can be translated.
        /// </summary>
        public string name;
        /// <summary>
        /// An optional hint for how to present this scope in the UI.
        /// If this attribute is missing, the scope is shown with a generic UI.
        /// Values: 'arguments': Scope contains method arguments.
        /// 'locals': Scope contains local variables.
        /// 'registers': Scope contains registers. Only a single 'registers' scope should be returned from a 'scopes' request.
        /// etc.
        /// </summary>
        public string presentationHint;
        /// <summary>
        /// The variables of this scope can be retrieved by passing the value of variablesReference to the VariablesRequest.
        /// </summary>
        public int variablesReference;
        /// <summary>
        /// The number of named variables in this scope.
        /// The client can use this optional information to present the variables in a paged UI and fetch them in chunks.
        /// </summary>
        public int namedVariables;
        /// <summary>
        /// The number of indexed variables in this scope.
        /// The client can use this optional information to present the variables in a paged UI and fetch them in chunks.
        /// </summary>
        public int indexedVariables;
        /// <summary>If true, the number of variables in this scope is large or expensive to retrieve.</summary>
        public bool expensive;
        // * OPTIONAL *
        // TODO: Source argument
        /// <summary>Optional start line of the range covered by this scope.</summary>
        public int line;
        /// <summary>Optional start column of the range covered by this scope.</summary>
        public int column;
        /// <summary>Optional end line of the range covered by this scope.</summary>
        public int endLine;
        /// <summary>Optional end column of the range covered by this scope.</summary>
        public int endColumn;
    }

    public class DBPVariable
    {
        public string name;
        public string value;
        public string type;
        public int variablesReference;
        public int namedVariables;
        public int indexedVariables;

        public DBPVariable() {}
        public DBPVariable(IDebugVariable variable)
        {
            name = variable.Name;
            type = variable.Type;
            value = variable.Value.ToString();
        }
    }
}