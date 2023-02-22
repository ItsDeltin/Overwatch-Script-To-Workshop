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

        /// <summary>Returns the location but with the uri as a String type rather than a Uri type.
        /// This exists because JObject.FromObject is sometimes used to encode data to pass to the clients
        /// of the language server. However, doing JObject.FromObject on a Uri will replace spaces with '%20', which
        /// the clients don't like very much. E.g for vscode in particular the references codelens (in CodeLens.cs) will
        /// only work for files the user as opened for some reason.</summary>
        public StringLocation AsStringLocation() => new StringLocation(uri.ToString(), range);
        public record struct StringLocation(string uri, DocRange range);
    }
}