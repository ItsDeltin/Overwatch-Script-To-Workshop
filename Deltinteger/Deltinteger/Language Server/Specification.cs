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

        public static implicit operator Pos(LSPos pos) => new Pos((int)pos.Line, (int)pos.Character);
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

        public static DocRange GetRange(object node)
        {
            if (node is ParserRuleContext context) return GetRange(context);
            if (node is ITerminalNode terminalNode) return GetRange(terminalNode);
            if (node is IToken token) return GetRange(token);

            throw new ArgumentException("Cannot get range of type '" + node.GetType().Name + "'.");
        }

        public static DocRange GetRange(object start, object stop) => new DocRange(DocRange.GetRange(start).start, DocRange.GetRange(stop).end);

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
        public static implicit operator DocRange(LSRange range) => new DocRange(range.Start, range.End);
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

    public class Location 
    {
        public Uri uri;
        public DocRange range;

        public Location(Uri uri, DocRange range)
        {
            this.uri = uri;
            this.range = range;
        }

        public LSLocation ToLsLocation()
        {
            return new LSLocation() {
                Uri = uri,
                Range = range.ToLsRange()
            };
        }
    }
}