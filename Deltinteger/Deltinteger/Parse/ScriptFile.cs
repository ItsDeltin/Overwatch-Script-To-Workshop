using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using LocationLink = OmniSharp.Extensions.LanguageServer.Protocol.Models.LocationLink;

namespace Deltin.Deltinteger.Parse
{
    public class ScriptFile
    {
        public DeltinScriptParser.RulesetContext Context { get; }
        public Uri Uri { get; }
        public FileDiagnostics Diagnostics { get; }
        public IToken[] Tokens { get; }

        private List<CompletionRange> completionRanges { get; } = new List<CompletionRange>();
        private List<OverloadChooser> overloads { get; } = new List<OverloadChooser>();
        private List<LocationLink> callLinks { get; } = new List<LocationLink>();

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

        public IToken NextToken(ITerminalNode token)
        {
            return Tokens[token.Symbol.TokenIndex + 1];
        }

        public void AddCompletionRange(CompletionRange completionRange)
        {
            completionRanges.Add(completionRange);
        }
        public CompletionRange[] GetCompletionRanges() => completionRanges.ToArray();

        public void AddOverloadData(OverloadChooser overload)
        {
            overloads.Add(overload);
        }
        public OverloadChooser[] GetSignatures() => overloads.ToArray();

        public void AddDefinitionLink(DocRange callRange, Location definedAt)
        {
            if (callRange == null) throw new ArgumentNullException(nameof(callRange));
            if (definedAt == null) throw new ArgumentNullException(nameof(definedAt));

            callLinks.Add(new LocationLink() {
                OriginSelectionRange = callRange.ToLsRange(),
                TargetUri = definedAt.uri,
                TargetRange = definedAt.range.ToLsRange(),
                TargetSelectionRange = definedAt.range.ToLsRange()
            });
        }
        public LocationLink[] GetDefinitionLinks() => callLinks.ToArray();
    }
}