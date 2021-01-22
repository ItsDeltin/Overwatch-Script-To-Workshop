using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Compiler.Parse;
using Deltin.Deltinteger.LanguageServer;
using TextDocumentItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.TextDocumentItem;

namespace Deltin.Deltinteger.Compiler
{
    public class Document
    {
        public Uri Uri { get; }
        public Lexer Lexer { get; }
        public string Content { get; private set; }
        public RootContext Syntax { get; private set; }
        public long Version { get; private set; }
        public List<IParserError> Errors { get; private set; }
        public ParserSettings ParserSettings { get; private set; }

        public Document(Uri uri, string initialContent)
        {
            Uri = uri;
            Lexer = new Lexer(new ParserSettings());
            Content = initialContent;
            Lexer.Init(new VersionInstance(Content));
            Parse();
        }

        public Document(TextDocumentItem document) : this(document.Uri.ToUri(), document.Text)
        {
            Version = document.Version;
        }

        private void Parse(IncrementInfo incrementInfo = null)
        {
            Parser parser = new Parser(Lexer, ParserSettings, Syntax, incrementInfo);
            Syntax = parser.Parse();
            Errors = parser.Errors;
        }

        public void Update(string newContent, UpdateRange updateRange, long version, ParserSettings parserSettings)
        {
            Version = version;
            Content = newContent;

            if (!parserSettings.Equals(ParserSettings))
            {
                ParserSettings = parserSettings;
                ParseFromScratch();
            }
            else
            {
                var increment = Lexer.Update(new VersionInstance(newContent), updateRange);
                Parse(increment);
            }
        }

        public void UpdateIfChanged(string newContent, ParserSettings parserSettings)
        {
            if (newContent == Content && parserSettings.Equals(ParserSettings)) return;
            Update(newContent, parserSettings);
        }

        public void Update(string newContent, ParserSettings parserSettings)
        {
            Content = newContent;
            ParserSettings = parserSettings;
            ParseFromScratch();
        }

        private void ParseFromScratch()
        {
            Syntax = null;
            Lexer.Reset();
            Lexer.Init(new VersionInstance(Content));
            Parse();
        }

        public Diagnostic[] GetDiagnostics() => Errors.Select(e => e.GetDiagnostic()).ToArray();

        public TextDocumentItem AsItem() => new TextDocumentItem()
        {
            Uri = Uri,
            Text = Content,
            LanguageId = "ostw"
        };
    }
}