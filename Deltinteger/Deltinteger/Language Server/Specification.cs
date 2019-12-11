using System;
using Deltin.Deltinteger.Parse;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using LSPos      = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;
using LSRange    = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using LSLocation = OmniSharp.Extensions.LanguageServer.Protocol.Models.Location;

namespace Deltin.Deltinteger.LanguageServer
{
    public class Pos : IComparable<Pos>
    {
        public static Pos Zero { get { return new Pos(0, 0); }}

        public int line { get; set; }
        public int character { get; set; }

        public Pos(int line, int character)
        {
            this.line = line;
            this.character = character;
        }

        public Pos() {}

        public override string ToString()
        {
            return line + ", " + character;
        }

        public DocRange ToRange()
        {
            return new DocRange(this, this);
        }

        public int CompareTo(Pos other)
        {
            if (other == null || this.line < other.line || (this.line == other.line && this.character < other.character))
                return -1;
            
            if (this.line == other.line && this.character == other.character)
                return 0;
            
            if (this.line > other.line || (this.line == other.line && this.character > other.character))
                return 1;

            throw new Exception();
        }

        #region Operators
        public static bool operator <(Pos p1, Pos p2)  => p1.CompareTo(p2) <  0;
        public static bool operator >(Pos p1, Pos p2)  => p1.CompareTo(p2) >  0;
        public static bool operator <=(Pos p1, Pos p2) => p1.CompareTo(p2) <= 0;
        public static bool operator >=(Pos p1, Pos p2) => p1.CompareTo(p2) >= 0;
        #endregion

        public Pos Offset(Pos other)
        {
            return new Pos(this.line + other.line, this.character + other.character);
        }

        public LSPos ToLsPos()
        {
            return new LSPos(line, character);
        }
    }

    public class DocRange : IComparable<DocRange>
    {
        public static DocRange Zero { get { return new DocRange(Pos.Zero, Pos.Zero); } }

        public Pos start { get; private set; }
        public Pos end { get; private set; }

        public DocRange(Pos start, Pos end)
        {
            this.start = start;
            this.end = end;
        }

        public static DocRange GetRange(ParserRuleContext context)
        {
            if (context.stop == null)
            {
                Pos pos = new Pos(context.start.Line, context.start.Column);
                return new DocRange(pos, pos);
            }

            if (context.start.Line == context.stop.Line &&
                context.start.Column == context.stop.Column)
            {
                return new DocRange
                (
                    new Pos(context.start.Line - 1, context.start.Column),
                    new Pos(context.stop.Line - 1, context.stop.Column + context.GetText().Length)
                );
            }
            else
            {
                return new DocRange
                (
                    new Pos(context.start.Line - 1, context.start.Column),
                    new Pos(context.stop.Line - 1, context.stop.Column + 1)
                );
            }
        }

        public static DocRange GetRange(IToken token)
        {
            return new DocRange(new Pos(token.Line - 1, token.Column), new Pos(token.Line - 1, token.Column + token.Text.Length));
        }

        public static DocRange GetRange(IToken start, IToken stop)
        {
            return new DocRange(new Pos(start.Line - 1, start.Column), new Pos(stop.Line - 1, stop.Column + stop.Text.Length));
        }

        public static DocRange GetRange(ITerminalNode start, ITerminalNode stop)
        {
            return GetRange(start.Symbol, stop.Symbol);
        }

        public static DocRange GetRange(ITerminalNode node)
        {
            return GetRange(node.Symbol);
        }

        public bool IsInside(Pos pos)
        {
            return (start.line < pos.line || (start.line == pos.line && pos.character >= start.character))
                && (end.line > pos.line || (end.line == pos.line && pos.character <= end.character));
        }

        public int CompareTo(DocRange other)
        {
            // Return -1 if 'this' is less than 'other'.
            // Return 0 if 'this' is equal to 'other'.
            // Return 1 if 'this' is greater than 'other'.

            // This is greater if other is null.
            if (other == null)
                return 1;

            // Get the number of lines 'start' and 'stop' contain.
            int thisLineDif = this.end.line - this.start.line; 
            int otherLineDif = other.end.line - other.start.line;

            // If 'this' has less lines than 'other', return less than.
            if (thisLineDif < otherLineDif)
                return -1;

            // If 'this' has more lines than 'other', return greater than.
            if (thisLineDif > otherLineDif)
                return 1;
            
            // If the amount of lines are equal, compare by character offset.
            if (thisLineDif == otherLineDif)
            {
                int thisCharDif = this.end.character - this.start.character;
                int otherCharDif = other.end.character - other.end.character;

                // Return less-than.
                if (thisCharDif < otherCharDif)
                    return -1;
                
                // Return equal.
                if (thisCharDif == otherCharDif)
                    return 0;

                // Return greater-than.
                if (thisCharDif > otherCharDif)
                    return 1;
            }

            // This isn't possible.
            throw new Exception();
        }

        #region Operators
        public static bool operator <(DocRange r1, DocRange r2)  => r1.CompareTo(r2) <  0;
        public static bool operator >(DocRange r1, DocRange r2)  => r1.CompareTo(r2) >  0;
        public static bool operator <=(DocRange r1, DocRange r2) => r1.CompareTo(r2) <= 0;
        public static bool operator >=(DocRange r1, DocRange r2) => r1.CompareTo(r2) >= 0;
        #endregion

        public DocRange Offset(DocRange other)
        {
            return new DocRange(start.Offset(other.start), end.Offset(other.end));
        }

        public override string ToString()
        {
            return start.ToString() + " - " + end.ToString();
        }

        public LSRange ToLsRange()
        {
            return new LSRange(start.ToLsPos(), end.ToLsPos());
        }
    }
    
    /*
    public class CompletionItem
    {
        #region Kinds
        public const int Text = 1;
        public const int Method = 2;
        public const int Function = 3;
        public const int Constructor = 4;
        public const int Field = 5;
        public const int Variable = 6;
        public const int Class = 7;
        public const int Interface = 8;
        public const int Module = 9;
        public const int Property = 10;
        public const int Unit = 11;
        public const int Value = 12;
        public const int Enum = 13;
        public const int Keyword = 14;
        public const int Snippet = 15;
        public const int Color = 16;
        public const int File = 17;
        public const int Reference = 18;
        public const int Folder = 19;
        public const int EnumMember = 20;
        public const int Constant = 21;
        public const int Struct = 22;
        public const int Event = 23;
        public const int Operator = 24;
        public const int TypeParameter = 25;
        #endregion

        public CompletionItem(string label)
        {
            this.label = label;
        }

        public string label;
        public int kind;
        public string detail;
        public object documentation;
        public bool deprecated;
        public string sortText;
        public string filterText;
        public int insertTextFormat;
        public TextEdit textEdit;
        public TextEdit[] additionalTextEdits;
        public string[] commitCharacters;
        public Command command;
        public object data;
    }

    class SignatureHelp
    {
        public SignatureInformation[] signatures;
        public int activeSignature;
        public int activeParameter;

        public SignatureHelp(SignatureInformation[] signatures, int activeSignature, int activeParameter)
        {
            this.signatures = signatures;
            this.activeSignature = activeSignature;
            this.activeParameter = activeParameter;
        }
    }

    class SignatureInformation
    {
        public string label;
        public object documentation; // string or markup
        public ParameterInformation[] parameters;

        public SignatureInformation(string label, object documentation, ParameterInformation[] parameters)
        {
            this.label = label;
            this.documentation = documentation;
            this.parameters = parameters;
        }
    }

    public class ParameterInformation
    {
        public object label; // string or int[]

        public object documentation; // string or markup

        public ParameterInformation(object label, object documentation)
        {
            this.label = label;
            this.documentation = documentation;
        }
    }

    public class PublishDiagnosticsParams
    {
        public string uri;
        public Diagnostic[] diagnostics;

        public PublishDiagnosticsParams(string uri, Diagnostic[] diagnostics)
        {
            this.uri = uri;
            this.diagnostics = diagnostics;
        }
    }
    */

    public class Diagnostic
    {
        public const int Error = 1;
        public const int Warning = 2;
        public const int Information = 3;
        public const int Hint = 4;
        
        public string message;
        public DocRange range;
        public int severity;
        public object code; // string or number
        public string source;
        public DiagnosticRelatedInformation[] relatedInformation;

        public Diagnostic(string message, DocRange range, int severity)
        {
            this.message = message;
            this.range = range;
            this.severity = severity;
        }

        override public string ToString()
        {
            return $"{DiagnosticSeverityText()} at {range.start.ToString()}: " + message;
        }
        public string Info(string file)
        {
            return $"{System.IO.Path.GetFileName(file)}: {DiagnosticSeverityText()} at {range.start.ToString()}: " + message;
        }

        private string DiagnosticSeverityText()
        {
            if (severity == 1)
                return "Error";
            else if (severity == 2)
                return "Warning";
            else if (severity == 3)
                return "Information";
            else if (severity == 4)
                return "Hint";
            else throw new Exception();
        }
    }

    public class DiagnosticRelatedInformation
    {
        public Location location;
        public string message;

        public DiagnosticRelatedInformation(Location location, string message)
        {
            this.location = location;
            this.message = message;
        }
    }

    /*
    // https://microsoft.github.io/language-server-protocol/specification#textDocument_hover
    class Hover
    {
        public object contents; // TODO MarkedString support 
        public DocRange range;

        public Hover(MarkupContent contents)
        {
            this.contents = contents;
        }
        #pragma warning disable 0618
        public Hover(MarkedString contents)
        {
            this.contents = contents;
        }
        public Hover(MarkedString[] contents)
        {
            this.contents = contents;
        }
        #pragma warning restore 0618
    }

    public class MarkupContent
    {
        public string kind;
        public string value;

        public const string PlainText = "plaintext";
        public const string Markdown = "markdown";

        public MarkupContent(string kind, string value)
        {
            this.kind = kind;
            this.value = value;
        }
    }

    [Obsolete("MarkedString is obsolete, use MarkupContent instead.")]
    public class MarkedString
    {
        public string language;
        public string value;

        public MarkedString(string language, string value)
        {
            this.language = language;
            this.value = value;
        }
    }
    */

    public class Location 
    {
        public Uri uri;
        public DocRange range;

        public Location(Uri uri, DocRange range)
        {
            this.uri = uri;
            this.range = range;
        }
    }

    /*
    class LocationLink
    {
        /// Span of the origin of this link.
        ///
        /// Used as the underlined span for mouse interaction. Defaults to the word range at
        /// the mouse position.
        public DocRange originSelectionRange;

        /// The target resource identifier of this link.
        public string targetUri;

        /// The full target range of this link. If the target for example is a symbol then target range is the
	    /// range enclosing this symbol not including leading/trailing whitespace but everything else
	    /// like comments. This information is typically used to highlight the range in the editor.
        public DocRange targetRange;

        /// The range that should be selected and revealed when this link is being followed, e.g the name of a function.
	    /// Must be contained by the the <see cref="targetRange"/>.
        public DocRange targetSelectionRange;

        public LocationLink(DocRange originSelectionRange, string targetUri, DocRange targetRange, DocRange targetSelectionRange)
        {
            this.originSelectionRange = originSelectionRange;
            this.targetUri = targetUri;
            this.targetRange = targetRange;
            this.targetSelectionRange = targetSelectionRange;
        }
    }

    public class TextEdit
    {
        public static TextEdit Replace(DocRange range, string newText)
        {
            return new TextEdit()
            {
                range = range,
                newText = newText
            };
        }
        public static TextEdit Insert(Pos pos, string newText)
        {
            return new TextEdit()
            {
                range = new DocRange(pos, pos),
                newText = newText
            };
        }
        public static TextEdit Delete(DocRange range)
        {
            return new TextEdit()
            {
                range = range,
                newText = string.Empty
            };
        }

        public DocRange range;
        public string newText;
    }

    public class Command
    {
        public string title;
        public string command;
        public object[] arguments;

        public Command(string title, string command)
        {
            this.title = title;
            this.command = command;
        }
    }
    */
}