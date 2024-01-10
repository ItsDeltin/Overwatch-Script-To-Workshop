using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Variables.Build;
using Deltin.Deltinteger.Parse.Variables.VanillaLink;

namespace Deltin.Deltinteger.Parse
{
    public class VarInfo
    {
        public string Name { get; }
        public Location DefinedAt { get; }
        public ParseInfo ParseInfo { get; }
        public VariableTypeHandler VariableTypeHandler { get; }

        public CodeType Type { get; set; } = null;
        public bool InferType { get; set; }
        public bool WholeContext { get; set; } = true;
        public bool Static { get; set; } = false;
        public bool InExtendedCollection { get; set; } = false;
        public bool Ref { get; set; }
        public int ID { get; set; } = -1;
        public IParseExpression InitialValueContext { get; set; } = null;
        public AccessLevel AccessLevel { get; set; } = AccessLevel.Private;
        public InitialValueResolve InitialValueResolve { get; set; } = InitialValueResolve.Instant;
        public Scope Scope { get; set; }
        public bool Recursive { get; set; }
        public SemanticTokenType TokenType { get; set; } = SemanticTokenType.Variable;
        public List<TokenModifier> TokenModifiers { get; set; } = new List<TokenModifier>();
        public bool HandleRestrictedCalls { get; set; }
        public CodeLensSourceType CodeLensType { get; set; } = CodeLensSourceType.Variable;
        public Lambda.IBridgeInvocable BridgeInvocable { get; set; }
        public bool RequiresCapture { get; set; }
        public bool IsMacro { get; set; }
        public bool Virtual { get; set; }
        public bool Override { get; set; }
        public bool Persist { get; set; }
        public IVariableInstance Overriding { get; set; }
        public MarkupBuilder Documentation { get; set; }
        public VariableLinkExpressionCollection LinkTargetVanilla { get; set; }

        public VarInfo(string name, Location definedAt, ParseInfo parseInfo)
        {
            Name = name;
            DefinedAt = definedAt;
            ParseInfo = parseInfo;
            VariableTypeHandler = new VariableTypeHandler(this);
        }
    }
}