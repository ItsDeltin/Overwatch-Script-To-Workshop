class JSONType: CodeType {
    JSONType[] children;

    public JSONType(JObject jsonData) : base("JSON"){
        foreach(JToken sibling in JObject){
            
        }
    }


    public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner) {

    }
}


class JSONConstructor : Constructor
{
        public JSONConstructor(JSONType jsonType) : base(jsonType, null, AccessLevel.Public)
        {
            Parameters = new CodeParameter[] {
                new JSONFileParameter("jsonFile", "path of the JSON file you want to access. Must be a `.json` file.")
            };
            Documentation = Extras.GetMarkupContent("Creates a macro out of a '.json' file.");
        }

        public override void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData) => throw new NotImplementedException();
    }

public class JSONFileParameter : FileParameter {
    public JSONFileParameter(string parameterName, string description) : base(parameterName, description, ".json") {}

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange){
            string filepath = base.Validate(script, value, valueRange) as string;
            if (filepath == null) return null;


            Jobject jsonData;

            try{
                ImportedScript file = FileGetter.GetImportedFile(filepath);
                file.Update();

                jsonData = JObject.parse(file.Content);
            } catch (InvalidOperationException){
                script.Diagnostics.Error("Failed to deserialize the JSON file.", valueRange);
                return null;
            }

            return jsonData;
        }

}