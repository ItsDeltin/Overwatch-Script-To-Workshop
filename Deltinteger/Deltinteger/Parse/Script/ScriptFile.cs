using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Parse.Overload;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using LocationLink = OmniSharp.Extensions.LanguageServer.Protocol.Models.LocationLink;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using ColorInformation = OmniSharp.Extensions.LanguageServer.Protocol.Models.ColorInformation;

namespace Deltin.Deltinteger.Parse
{
    public class ScriptFile
    {
        public Uri Uri => Document.Uri;
        public RootContext Context => Document.Syntax;
        public FileDiagnostics Diagnostics { get; }
        public Document Document { get; }
        public ScriptElements Elements { get; } = new ScriptElements();

        readonly List<CompletionRange> _completionRanges = new List<CompletionRange>();
        readonly List<OverloadChooser> _overloads = new List<OverloadChooser>();
        readonly List<LocationLink> _callLinks = new List<LocationLink>();
        readonly List<HoverRange> _hoverRanges = new List<HoverRange>();
        readonly List<CodeLensRange> _codeLensRanges = new List<CodeLensRange>();
        readonly List<SemanticToken> _semanticTokens = new List<SemanticToken>();
        readonly List<ColorInformation> _colorRanges = new List<ColorInformation>();

        public ScriptFile(Diagnostics diagnostics, Document document)
        {
            Document = document;
            Diagnostics = diagnostics.FromUri(Uri);
            Diagnostics.AddDiagnostics(document.GetDiagnostics());
            Document.Cache.EndCycle();
        }
        public ScriptFile(Diagnostics diagnostics, Uri uri, string content) : this(diagnostics, new Document(uri, content))
        {
        }

        public Token NextToken(Token token) => Document.Lexer.Tokens[Document.Lexer.Tokens.IndexOf(token) + 1];
        public bool IsTokenLast(Token token) => Document.Lexer.Tokens.Count - 1 == Document.Lexer.Tokens.IndexOf(token);

        public Location GetLocation(DocRange range) => new Location(Uri, range);

        public void AddCompletionRange(CompletionRange completionRange) => _completionRanges.Add(completionRange);
        public CompletionRange[] GetCompletionRanges() => _completionRanges.ToArray();

        public void AddOverloadData(OverloadChooser overload) => _overloads.Add(overload);
        public OverloadChooser[] GetSignatures() => _overloads.ToArray();

        /// <summary>Adds a link that can be clicked on in the script.</summary>
        public void AddDefinitionLink(DocRange source, Location target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            _callLinks.Add(new LocationLink()
            {
                OriginSelectionRange = source,
                TargetUri = target.uri.ToDefinition(),
                TargetRange = target.range,
                TargetSelectionRange = target.range
            });
        }
        public LocationLink[] GetDefinitionLinks() => _callLinks.ToArray();

        ///<summary>Adds a hover to the file.</summary>
        public void AddHover(DocRange range, MarkupBuilder content)
        {
            if (range == null) throw new ArgumentNullException(nameof(range));
            if (content == null) throw new ArgumentNullException(nameof(content));

            _hoverRanges.Add(new HoverRange(range, content));
        }
        public HoverRange[] GetHoverRanges() => _hoverRanges.ToArray();

        ///<summary>Adds a codelens to the file.</summary>
        public void AddCodeLensRange(CodeLensRange codeLensRange) => _codeLensRanges.Add(codeLensRange ?? throw new ArgumentNullException(nameof(codeLensRange)));
        public CodeLensRange[] GetCodeLensRanges() => _codeLensRanges.ToArray();

        /// <summary>Adds a semantic token to the file.</summary>
        public void AddToken(DocRange range, SemanticTokenType type, params TokenModifier[] modifiers) => AddToken(new SemanticToken(range, type, modifiers));
        /// <summary>Adds a semantic token to the file.</summary>
        public void AddToken(SemanticToken token) => _semanticTokens.Add(token);
        public SemanticToken[] GetSemanticTokens() => _semanticTokens.ToArray();

        public void AddColorRange(ColorInformation colorRange) => _colorRanges.Add(colorRange);
        public ColorInformation[] GetColorRanges() => _colorRanges.ToArray();
    }

    public class CompletionRange
    {
        public DocRange Range { get; }
        public CompletionRangeKind Kind { get; }
        private readonly DeltinScript _deltinScript;
        private readonly Scope _scope;
        private readonly CodeType _getter;
        private readonly CompletionItem[] _completionItems;

        public CompletionRange(DeltinScript deltinScript, Scope scope, DocRange range, CompletionRangeKind kind)
        {
            _deltinScript = deltinScript;
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Kind = kind;
            Range = range;
        }

        public CompletionRange(DeltinScript deltinScript, Scope scope, CodeType getter, DocRange range, CompletionRangeKind kind)
        {
            _deltinScript = deltinScript;
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            _getter = getter;
            Kind = kind;
            Range = range;
        }

        public CompletionRange(DeltinScript deltinScript, CompletionItem[] completionItems, DocRange range, CompletionRangeKind kind)
        {
            _deltinScript = deltinScript;
            _completionItems = completionItems ?? throw new ArgumentNullException(nameof(completionItems));
            Kind = kind;
            Range = range;
        }

        public CompletionItem[] GetCompletion(DocPos pos, bool immediate)
        {
            if (_scope == null) return _completionItems;
            return _scope.GetCompletion(_deltinScript, pos, immediate, _getter);
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
        public MarkupBuilder Content { get; }

        public HoverRange(DocRange range, MarkupBuilder content)
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

        public SemanticToken(DocRange range, SemanticTokenType tokenType, params TokenModifier[] modifiers)
        {
            Range = range;
            TokenType = GetTokenName(tokenType);
            Modifiers = modifiers == null ? new string[0] : Array.ConvertAll(modifiers, modifier => GetModifierName(modifier));
        }

        private static string GetTokenName(SemanticTokenType tokenType)
        {
            switch (tokenType)
            {
                case Deltin.Deltinteger.Parse.SemanticTokenType.TypeParameter: return "typeParameter";
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

    public enum SemanticTokenType
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