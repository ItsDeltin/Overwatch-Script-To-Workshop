using System;
using System.Linq;
using System.Collections.Generic;
using DS.Analysis.Diagnostics;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.Parse;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis
{
    class FileParser : IDisposable
    {
        public RootContext Syntax { get; private set; }
        readonly Lexer lexer = new Lexer();
        readonly ScriptFile file;

        IEnumerable<Diagnostic> parserDiagnostics = Enumerable.Empty<Diagnostic>();


        public FileParser(ScriptFile file)
        {
            this.file = file;
        }


        public FileUpdater GetFileUpdater() => new FileUpdater(this);

        public void SetFromString(string content)
        {
            lexer.Init(new VersionInstance(content));
            Parse();
            file.GetStructure();
        }

        void Parse()
        {
            var parser = new Parser(lexer);
            Syntax = parser.Parse();

            Dispose();
            parserDiagnostics = parser.Errors.Select(error => file.Diagnostics.Error(error.Message(), error.Range)).ToArray();
        }


        public void Dispose()
        {
            foreach (var diagnostic in parserDiagnostics)
                 diagnostic.Dispose();
        }


        /// <summary>Incremental script updates.</summary>
        public class FileUpdater
        {
            readonly FileParser fileParser;
            public FileUpdater(FileParser fileParser) => this.fileParser = fileParser;
            public void Update(UpdateRange change)
            {
                fileParser.lexer.Update(fileParser.lexer.Content.Update(change), change);
                fileParser.Parse();
            }
            public void ApplyUpdates() => fileParser.file.GetStructure();
        }
    }
}