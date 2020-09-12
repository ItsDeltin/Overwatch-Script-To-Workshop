using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse.Lambda
{
    public class LambdaContextHandler : IVarContextHandler
    {
        public ParseInfo ParseInfo { get; }
        private readonly LambdaParameter _parameter;

        public LambdaContextHandler(ParseInfo parseInfo, LambdaParameter parameter)
        {
            ParseInfo = parseInfo;
            _parameter = parameter;
        }

        public VarBuilderAttribute[] GetAttributes() => new VarBuilderAttribute[0];
        public ParseType GetCodeType() => _parameter.Type;
        public Location GetDefineLocation() => new Location(ParseInfo.Script.Uri, _parameter.Identifier.Range);
        public string GetName() => _parameter.Identifier.Text;
        public DocRange GetNameRange() => _parameter.Identifier.Range;
        public DocRange GetTypeRange() => _parameter.Type.Range;
        public bool CheckName() => _parameter.Identifier;
    }
}