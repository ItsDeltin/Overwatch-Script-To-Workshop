using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    public class ScriptFile
    {
        public DeltinScriptParser.RulesetContext Context { get; }
        public string File { get; }
        public FileDiagnostics Diagnostics { get; }

        public ScriptFile(Diagnostics diagnostics, string file, string content)
        {
            File = file;
            AntlrInputStream inputStream = new AntlrInputStream(content);

            // Lexer
            DeltinScriptLexer lexer = new DeltinScriptLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);

            Diagnostics = diagnostics.FromFile(File);

            // Parse
            DeltinScriptParser parser = new DeltinScriptParser(commonTokenStream);
            var errorListener = new ErrorListener(Diagnostics);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            Context = parser.ruleset();
            // AdditionalErrorChecking aec = new AdditionalErrorChecking(file, parser, diagnostics);
            // aec.Visit(Context);
        }
    }
}