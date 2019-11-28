using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Models;
using Deltin.Deltinteger.Pathfinder;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    public class ScriptFile
    {
        public DeltinScriptParser.RulesetContext Context { get; }
        public string File { get; }
        public string Content { get; }
        public FileDiagnostics Diagnostics { get; }

        public ScriptFile(Diagnostics diagnostics, string file, string content)
        {
            File = file;
            Content = content;
            AntlrInputStream inputStream = new AntlrInputStream(content);

            // Lexer
            DeltinScriptLexer lexer = new DeltinScriptLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);

            // Parse
            DeltinScriptParser parser = new DeltinScriptParser(commonTokenStream);
            var errorListener = new ErrorListener(file, diagnostics);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            Context = parser.ruleset();
            AdditionalErrorChecking aec = new AdditionalErrorChecking(file, parser, diagnostics);
            aec.Visit(Context);

            Diagnostics = diagnostics.FromFile(File);
        }
    }
}