using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Parse.Overload;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using LocationLink = OmniSharp.Extensions.LanguageServer.Protocol.Models.LocationLink;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class ScriptFile
    {
        public Uri Uri => Document.Uri;
        public RootContext Context => Document.Syntax;
        public FileDiagnostics Diagnostics { get; }
        public Document Document { get; }

        private List<CompletionRange> completionRanges { get; } = new List<CompletionRange>();
        private List<OverloadChooser> overloads { get; } = new List<OverloadChooser>();
        private List<LocationLink> callLinks { get; } = new List<LocationLink>();
        private List<HoverRange> hoverRanges { get; } = new List<HoverRange>();
        private List<CodeLensRange> codeLensRanges { get; } = new List<CodeLensRange>();
        private List<SemanticToken> semanticTokens { get; } = new List<SemanticToken>();

        public ScriptFile(Diagnostics diagnostics, Document document)
        {
            Document = document;
            Diagnostics = diagnostics.FromUri(Uri);
            Diagnostics.AddDiagnostics(document.GetDiagnostics());
        }
        public ScriptFile(Diagnostics diagnostics, Uri uri, string content) : this(diagnostics, new Document(uri, content))
        {
        }

        public Token NextToken(Token token) => Document.Lexer.Tokens[Document.Lexer.Tokens.IndexOf(token) + 1];
        public bool IsTokenLast(Token token) => Document.Lexer.Tokens.Count - 1 == Document.Lexer.Tokens.IndexOf(token);

        public Location GetLocation(DocRange range) => new Location(Uri, range);

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

        /// <summary>Adds a link that can be clicked on in the script.</summary>
        public void AddDefinitionLink(DocRange callRange, Location definedAt)
        {
            if (callRange == null) throw new ArgumentNullException(nameof(callRange));
            if (definedAt == null) throw new ArgumentNullException(nameof(definedAt));

            callLinks.Add(new LocationLink() {
                OriginSelectionRange = callRange,
                TargetUri = definedAt.uri,
                TargetRange = definedAt.range,
                TargetSelectionRange = definedAt.range
            });
        }
        public LocationLink[] GetDefinitionLinks() => callLinks.ToArray();

        ///<summary>Adds a hover to the file.</summary>
        public void AddHover(DocRange range, string content)
        {
            if (range == null) throw new ArgumentNullException(nameof(range));
            if (content == null) throw new ArgumentNullException(nameof(content));

            hoverRanges.Add(new HoverRange(range, content));
        }
        public HoverRange[] GetHoverRanges() => hoverRanges.ToArray();

        ///<summary>Adds a codelens to the file.</summary>
        public void AddCodeLensRange(CodeLensRange codeLensRange)
        {
            codeLensRanges.Add(codeLensRange ?? throw new ArgumentNullException(nameof(codeLensRange)));
        }
        public CodeLensRange[] GetCodeLensRanges() => codeLensRanges.ToArray();

        /// <summary>Adds a semantic token to the file.</summary>
        public void AddToken(DocRange range, TokenType type, params TokenModifier[] modifiers) => AddToken(new SemanticToken(range, type, modifiers));
        /// <summary>Adds a semantic token to the file.</summary>
        public void AddToken(SemanticToken token) => semanticTokens.Add(token);
        public SemanticToken[] GetSemanticTokens() => semanticTokens.ToArray();
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

        public CompletionItem[] GetCompletion(DocPos pos, bool immediate)
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

    public class SemanticToken
    {
        public DocRange Range { get; }
        public string TokenType { get; }
        public string[] Modifiers { get; }

        public SemanticToken(DocRange range, TokenType tokenType, params TokenModifier[] modifiers)
        {
            Range = range;
            TokenType = GetTokenName(tokenType);
            Modifiers = modifiers == null ? new string[0] : Array.ConvertAll(modifiers, modifier => GetModifierName(modifier));
        }

        private static string GetTokenName(TokenType tokenType)
        {
            switch (tokenType)
            {
                case Deltin.Deltinteger.Parse.TokenType.TypeParameter: return "typeParameter";
                default: return tokenType.ToString().ToLower();
            }
        }

        private static string GetModifierName(TokenModifier modifier)
        {
            switch (modifier)
            {
                case TokenModifier.DefaultLibrary: return "defaultLibrary";
                default: return modifier.ToString().ToLower();
            }
        }
    }

    public enum TokenType
    {
        Namespace,
        Type, Class, Enum, Interface, Struct, TypeParameter,
        Parameter, Variable, Property, EnumMember, Event,
        Function, Member, Macro,
        Label,
        Comment, String, Keyword, Number, Regexp, Operator
    }

    public enum TokenModifier
    {
        Declaration,
        Readonly, Static, Deprecated, Abstract,
        Async, Modification, Documentation, DefaultLibrary
    }
}