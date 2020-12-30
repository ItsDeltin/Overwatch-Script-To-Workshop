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
        public static readonly EvaluateResponse Empty = new EvaluateResponse();

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

        public EvaluateResponse() { }
        public EvaluateResponse(DebugVariableLinkCollection collection, IDebugVariable variable)
        {
            type = variable.Type;
            result = variable.Value.ToString();
            collection.References.TryGetValue(variable, out variablesReference);
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
        /// <summary>The variable's name.</summary>
        public string name;
        /// <summary>The variable's value. This can be a multi-line text, e.g. for a function the body of a function.</summary>
        public string value;
        /// <summary>The type of the variable's value. Typically shown in the UI when hovering over the value.
        /// This attribute should only be returned by a debug adapter if the client has passed the value true for the 'supportsVariableType' capability of the 'initialize' request.</summary>
        public string type;
        /// <summary>Properties of a variable that can be used to determine how to render the variable in the UI.</summary>
        public VariablePresentationHint presentationHint;
        /// <summary>Optional evaluatable name of this variable which can be passed to the 'EvaluateRequest' to fetch the variable's value.</summary>
        public string evaluateName;
        /// <summary>If variablesReference is > 0, the variable is structured and its children can be retrieved by passing variablesReference to the VariablesRequest.</summary>
        public int variablesReference;
        /// <summary>The number of named child variables.
        /// The client can use this optional information to present the children in a paged UI and fetch them in chunks.</summary>
        public int namedVariables;
        /// <summary>The number of indexed child variables.
        /// The client can use this optional information to present the children in a paged UI and fetch them in chunks.</summary>
        public int indexedVariables;
        /// <summary>Optional memory reference for the variable if the variable represents executable code, such as a function pointer.
        /// This attribute is only required if the client has passed the value true for the 'supportsMemoryReferences' capability of the 'initialize' request.</summary>
        public string memoryReference;

        public DBPVariable() { }
        public DBPVariable(IDebugVariable variable, string typeString = null)
        {
            name = variable.Name;
            type = variable.Type;
            value = variable.Value.ToString();
            if (typeString != null) value += " {" + typeString + "}";
        }
    }

    public class VariablePresentationHint
    {
        /// <summary>
        /// The kind of variable. Before introducing additional values, try to use the listed values.
        /// Values: 
        /// 'property': Indicates that the object is a property.
        /// 'method': Indicates that the object is a method.
        /// 'class': Indicates that the object is a class.
        /// 'data': Indicates that the object is data.
        /// 'event': Indicates that the object is an event.
        /// 'baseClass': Indicates that the object is a base class.
        /// 'innerClass': Indicates that the object is an inner class.
        /// 'interface': Indicates that the object is an interface.
        /// 'mostDerivedClass': Indicates that the object is the most derived class.
        /// 'virtual': Indicates that the object is virtual, that means it is a synthetic object introducedby the
        /// adapter for rendering purposes, e.g. an index range for large arrays.
        /// 'dataBreakpoint': Indicates that a data breakpoint is registered for the object.
        /// etc.
        /// </summary>
        public string kind;
        /// <summary>
        /// Set of attributes represented as an array of strings. Before introducing additional values, try to use the listed values.
        /// Values: 
        /// 'static': Indicates that the object is static.
        /// 'constant': Indicates that the object is a constant.
        /// 'readOnly': Indicates that the object is read only.
        /// 'rawString': Indicates that the object is a raw string.
        /// 'hasObjectId': Indicates that the object can have an Object ID created for it.
        /// 'canHaveObjectId': Indicates that the object has an Object ID associated with it.
        /// 'hasSideEffects': Indicates that the evaluation had side effects.
        /// etc.
        /// </summary>
        public string attributes;
        /// <summary>Visibility of variable. Before introducing additional values, try to use the listed values.
        /// Values: 'public', 'private', 'protected', 'internal', 'final', etc.</summary>
        public string visibility;
    }
}