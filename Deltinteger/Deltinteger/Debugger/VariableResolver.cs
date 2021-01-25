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
        public DBPVariable GetVariable(DebugVariableLinkCollection collection, IDebugVariable debugVariable)
        {
            // Return null if there is no value.
            if (debugVariable.Value == null) return null;

            // Create the variable.
            DBPVariable variable = GetProtocolVariable(debugVariable);

            // Add the clipboard value copy.
            variable.evaluateName = collection.AddClipboardKey(debugVariable.Name, debugVariable.Value.AsOSTWExpression());

            // If the value is an array, assign a reference.
            if (debugVariable.Value is CsvArray array)
            {
                variable.indexedVariables = array.Values.Length;
                variable.variablesReference = IDebugVariable.ApplyReference(collection, debugVariable);
            }
            // If the value is a vector, assign a reference.
            else if (debugVariable.Value is CsvVector vector)
            {
                variable.namedVariables = 3; // X, Y, and Z.
                variable.variablesReference = IDebugVariable.ApplyReference(collection, debugVariable);
            }

            return variable;
        }
        public EvaluateResponse GetEvaluation(DebugVariableLinkCollection collection, IDebugVariable debugVariable)
        {
            // Return null if there is no value.
            if (debugVariable.Value == null) return EvaluateResponse.Empty;

            // Create the evaluation response.
            EvaluateResponse response = new EvaluateResponse(collection, debugVariable);

            // Array
            if (debugVariable.Value is CsvArray array)
            {
                response.indexedVariables = array.Values.Length;
                response.variablesReference = IDebugVariable.ApplyReference(collection, debugVariable);
            }
            // Vector
            else if (debugVariable.Value is CsvVector)
            {
                response.namedVariables = 3;
                response.variablesReference = IDebugVariable.ApplyReference(collection, debugVariable);
            }

            return response;
        }

        public virtual IDebugVariable[] GetChildren(DebugVariableLinkCollection collection, IDebugVariable parent)
        {
            // Get the array.
            if (parent.Value is CsvArray array)
            {
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
            // Get the vector.
            else if (parent.Value is CsvVector vector)
            {
                var x = GetChildDebugVariable(collection, new CsvNumber(vector.Value.X), "X");
                var y = GetChildDebugVariable(collection, new CsvNumber(vector.Value.Y), "Y");
                var z = GetChildDebugVariable(collection, new CsvNumber(vector.Value.Z), "Z");
                collection.Add(x);
                collection.Add(y);
                collection.Add(z);
                return new IDebugVariable[] { x, y, z };
            }
            return new LinkableDebugVariable[0];
        }

        protected virtual DBPVariable GetProtocolVariable(IDebugVariable variable) => new DBPVariable(variable);
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

        protected override DBPVariable GetProtocolVariable(IDebugVariable variable) => new DBPVariable(variable, _arrayOfTypeName + "[]");
        protected override ChildDebugVariable GetChildDebugVariable(DebugVariableLinkCollection collection, CsvPart arrayValue, string indexName) => new ChildDebugVariable(_typeResolver, arrayValue, indexName, _arrayOfTypeName);
    }
}