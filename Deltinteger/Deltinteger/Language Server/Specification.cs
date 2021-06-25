using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;
using LSPos = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;
using LSRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using LSLocation = OmniSharp.Extensions.LanguageServer.Protocol.Models.Location;

namespace Deltin.Deltinteger.LanguageServer
{
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
            this.message = message ?? throw new NullReferenceException(nameof(message));
            this.range = range ?? throw new NullReferenceException(nameof(range));
            this.severity = severity;
        }

        override public string ToString()
        {
            return $"{DiagnosticSeverityText()} at {range.Start.ToString()}: " + message;
        }
        public string Info(string file)
        {
            return $"{System.IO.Path.GetFileName(file)}: {DiagnosticSeverityText()} at {range.Start.ToString()}: " + message;
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
            this.uri = uri ?? throw new ArgumentNullException(nameof(uri));
            this.range = range ?? throw new ArgumentNullException(nameof(range));
        }

        public LSLocation ToLsLocation()
        {
            return new LSLocation()
            {
                Uri = uri,
                Range = range
            };
        }
    }
}