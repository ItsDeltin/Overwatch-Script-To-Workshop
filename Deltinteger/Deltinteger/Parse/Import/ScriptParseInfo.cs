using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    public class ScriptParseInfo
    {
        public DeltinScriptParser.RulesetContext Context { get; private set; }
        public List<Diagnostic> StructuralDiagnostics { get; private set; }
        public IToken[] Tokens { get; private set; }

        public ScriptParseInfo() {}

        public ScriptParseInfo(string content)
        {
            Update(content);
        }

        public void Update(string content)
        {
            AntlrInputStream inputStream = new AntlrInputStream(content);

            // Lexer
            DeltinScriptLexer lexer = new DeltinScriptLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            commonTokenStream.Fill();
            Tokens = commonTokenStream.GetTokens().ToArray();
            commonTokenStream.Reset();

            // Parse
            DeltinScriptParser parser = new DeltinScriptParser(commonTokenStream);
            var errorListener = new ErrorListener();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            StructuralDiagnostics = errorListener.Diagnostics;
            Context = parser.ruleset();
        }
    }
}