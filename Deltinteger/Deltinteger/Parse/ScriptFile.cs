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
        public Uri Uri { get; }
        public FileDiagnostics Diagnostics { get; }
        public IToken[] Tokens { get; }
        private List<CompletionRange> completionRanges { get; } = new List<CompletionRange>();
        private List<SignatureRange> signatureRanges { get; } = new List<SignatureRange>();

        public ScriptFile(Diagnostics diagnostics, Uri uri, string content)
        {
            Uri = uri;
            AntlrInputStream inputStream = new AntlrInputStream(content);

            // Lexer
            DeltinScriptLexer lexer = new DeltinScriptLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            commonTokenStream.Fill();
            Tokens = commonTokenStream.GetTokens().ToArray();
            commonTokenStream.Reset();

            Diagnostics = diagnostics.FromUri(Uri);

            // Parse
            DeltinScriptParser parser = new DeltinScriptParser(commonTokenStream);
            var errorListener = new ErrorListener(Diagnostics);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            Context = parser.ruleset();
        }

        public void AddCompletionRange(CompletionRange completionRange)
        {
            completionRanges.Add(completionRange);
        }
        public CompletionRange[] GetCompletionRanges() => completionRanges.ToArray();

        public void AddSignatureRange(SignatureRange signatureRange)
        {
            signatureRanges.Add(signatureRange);
        }
        public SignatureRange[] GetSignatureRanges() => signatureRanges.ToArray();
    }
}