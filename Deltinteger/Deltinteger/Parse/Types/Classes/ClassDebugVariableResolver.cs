using System.Linq;
using Deltin.Deltinteger.Debugger;
using Deltin.Deltinteger.Debugger.Protocol;
using Deltin.Deltinteger.Csv;

namespace Deltin.Deltinteger.Parse
{
    /// <summary>Links variables with the type of a class with debugger variables discovered from the action stream.</summary>
    public class ClassDebugVariableResolver : IDebugVariableResolver
    {
        public ClassType Class { get; }

        public ClassDebugVariableResolver(ClassType @class)
        {
            Class = @class;
        }

        public DBPVariable GetVariable(DebugVariableLinkCollection collection, IDebugVariable debugVariable)
        {
            // Return null if there is no value.
            if (debugVariable.Value == null) return null;

            // Create the variable.
            DBPVariable variable = new DBPVariable(debugVariable, Class.Name);
            variable.namedVariables = Class.ObjectVariables.Length;
            variable.variablesReference = IDebugVariable.ApplyReference(collection, debugVariable);

            return variable;
        }

        public EvaluateResponse GetEvaluation(DebugVariableLinkCollection collection, IDebugVariable debugVariable)
        {
            // Return null if there is no value.
            if (debugVariable.Value == null) return EvaluateResponse.Empty;

            // Create the evaluation response.
            IDebugVariable.ApplyReference(collection, debugVariable);
            EvaluateResponse response = new EvaluateResponse(collection, debugVariable);
            response.namedVariables = Class.ObjectVariables.Length;
            
            return response;
        }

        public IDebugVariable[] GetChildren(DebugVariableLinkCollection collection, IDebugVariable parent)
        {
            // Use the default resolver if the value is not a number.
            if (parent.Value is CsvNumber == false)
                return new DefaultResolver().GetChildren(collection, parent);

            // The class reference of the parent variable.
            int reference = (int)((CsvNumber)parent.Value).Value;

            IDebugVariable[] variables = new IDebugVariable[Class.ObjectVariables.Length];
            for (int i = 0; i < variables.Length; i++)
            {
                CsvPart value = new CsvNull();

                // Get the related object variable array.
                var objectVariableArray = collection.ActionStream.Variables.FirstOrDefault(v => v.Name == ClassData.ObjectVariableTag + i);
                if (objectVariableArray != null && objectVariableArray.Value is Csv.CsvArray csvArray && reference < csvArray.Values.Length)
                    value = csvArray.Values[reference];

                variables[i] = new ChildDebugVariable(
                    // Child variable resolver
                    Class.ObjectVariables[i].Variable.CodeType.DebugVariableResolver ?? new DefaultResolver(),
                    // Value
                    value,
                    // Name
                    Class.ObjectVariables[i].Variable.Name,
                    // Type
                    Class.ObjectVariables[i].Variable.CodeType.GetName()
                );
                collection.Add(variables[i]);
            }

            return variables;
        }
    }
}