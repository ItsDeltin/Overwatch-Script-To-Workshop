using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using LocationLink = OmniSharp.Extensions.LanguageServer.Protocol.Models.LocationLink;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class ScriptFile
    {
        public Uri Uri { get; }
        public FileDiagnostics Diagnostics { get; }

        private ScriptParseInfo ScriptParseInfo { get; }
        public DeltinScriptParser.RulesetContext Context { get; }
        public IToken[] Tokens { get; }

        private List<CompletionRange> completionRanges { get; } = new List<CompletionRange>();
        private List<OverloadChooser> overloads { get; } = new List<OverloadChooser>();
        private List<LocationLink> callLinks { get; } = new List<LocationLink>();
        private List<HoverRange> hoverRanges { get; } = new List<HoverRange>();

        public ScriptFile(Diagnostics diagnostics, Uri uri, ScriptParseInfo scriptParseInfo)
        {
            Uri = uri;
            Diagnostics = diagnostics.FromUri(Uri);
            Diagnostics.AddDiagnostics(scriptParseInfo.StructuralDiagnostics.ToArray());
            ScriptParseInfo = scriptParseInfo;
            Context = ScriptParseInfo.Context;
            Tokens = scriptParseInfo.Tokens;
        }
        public ScriptFile(Diagnostics diagnostics, Uri uri, string content) : this(diagnostics, uri, new ScriptParseInfo(content))
        {
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

        public void AddHover(DocRange range, string content)
        {
            if (range == null) throw new ArgumentNullException(nameof(range));
            if (content == null) throw new ArgumentNullException(nameof(content));

            hoverRanges.Add(new HoverRange(range, content));
        }
        public HoverRange[] GetHoverRanges() => hoverRanges.ToArray();
    }

    public class CompletionRange
    {
        private Scope Scope { get; }
        private Scope Getter { get; }
        private CompletionItem[] CompletionItems { get; }
        public DocRange Range { get; }
        public CompletionRangeKind Kind { get; }

        public CompletionRange(Scope scope, DocRange range, CompletionRangeKind kind)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Kind = kind;
            Range = range;
        }

        public CompletionRange(Scope scope, Scope getter, DocRange range, CompletionRangeKind kind)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Getter = getter;
            Kind = kind;
            Range = range;
        }

        public CompletionRange(CompletionItem[] completionItems, DocRange range, CompletionRangeKind kind)
        {
            CompletionItems = completionItems ?? throw new ArgumentNullException(nameof(completionItems));
            Kind = kind;
            Range = range;
        }

        public CompletionItem[] GetCompletion(Pos pos, bool immediate)
        {
            return Scope?.GetCompletion(pos, immediate, Getter) ?? CompletionItems;

        }
    }

    public enum CompletionRangeKind
    {
        Additive,
        Catch,
        ClearRest
    }

    public class HoverRange
    {
        public DocRange Range { get; }
        public string Content { get; }

        public HoverRange(DocRange range, string content)
        {
            Range = range;
            Content = content;
        }
    }
}