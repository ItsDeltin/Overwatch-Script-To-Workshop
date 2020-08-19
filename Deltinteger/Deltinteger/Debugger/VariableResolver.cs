using Deltin.Deltinteger.Debugger.Protocol;
using Deltin.Deltinteger.Csv;

namespace Deltin.Deltinteger.Debugger
{
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
                children[i] = GetChildDebugVariable(collection, array.Values[i], "[" + i + "]");
                collection.Add(children[i]);
            }
            
            // Done
            return children;
        }

        protected virtual ChildDebugVariable GetChildDebugVariable(DebugVariableLinkCollection collection, CsvPart arrayValue, string indexName) => new ChildDebugVariable(this, arrayValue, indexName, "define");
    }

    class ArrayResolver : DefaultResolver
    {
        private readonly IDebugVariableResolver _typeResolver;
        private readonly string _arrayOfTypeName;
        private readonly bool _typeContainsChildren;

        public ArrayResolver(IDebugVariableResolver typeResolver, string arrayOfTypeName, bool typeContainsChildren)
        {
            _typeResolver = typeResolver ?? new DefaultResolver();
            _arrayOfTypeName = arrayOfTypeName ?? "define";
            _typeContainsChildren = typeContainsChildren;
        }

        protected override ChildDebugVariable GetChildDebugVariable(DebugVariableLinkCollection collection, CsvPart arrayValue, string indexName) => new ChildDebugVariable(_typeResolver, arrayValue, indexName, _arrayOfTypeName);
    }
}