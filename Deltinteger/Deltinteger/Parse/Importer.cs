using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Deltin.Deltinteger.Parse
{
    public class FileGetter
    {
        private DocumentHandler DocumentHandler { get; }
        // Importing scripts not being edited.
        private List<ImportedFile> ImportedFiles = new List<ImportedFile>();
        // Importing script being edited.
        private List<DocumentScript> EditingImportedFiles = new List<DocumentScript>();

        public FileGetter(DocumentHandler documentHandler)
        {
            DocumentHandler = documentHandler;
        }

        public ScriptParseInfo GetScript(Uri uri)
        {
            string doc = DocumentHandler?.Documents.FirstOrDefault(td => td.Uri == uri)?.Text;

            if (doc != null)
            {
                DocumentScript importedFile = GetImportedEditingFile(uri);
                importedFile.Update(doc);
                return importedFile.ScriptParseInfo;
            }
            else
            {
                ImportedScript importedFile = GetImportedFile(uri);
                importedFile.Update();
                return importedFile.ScriptParseInfo ?? throw new ArgumentNullException(nameof(importedFile.ScriptParseInfo));
            }
        }

        private ImportedScript GetImportedFile(Uri uri)
        {
            foreach (ImportedScript importedFile in ImportedFiles)
                if (importedFile.Uri == uri)
                    return importedFile;
            var newImportedFile = new ImportedScript(uri);
            ImportedFiles.Add(newImportedFile);
            return newImportedFile;
        }

        private DocumentScript GetImportedEditingFile(Uri uri)
        {
            foreach (DocumentScript importedFile in EditingImportedFiles)
                if (importedFile.Uri == uri)
                    return importedFile;
            var newImportedFile = new DocumentScript(uri);
            EditingImportedFiles.Add(newImportedFile);
            return newImportedFile;
        }
    }

    class ImportedFile
    {
        public Uri Uri { get; }
        public string Content { get; private set; }
        private byte[] Hash { get; set; }

        public ImportedFile(Uri uri)
        {
            Uri = uri;
            
            using (FileStream stream = GetStream())
            {
                Hash = GetFileHash(stream);
                stream.Position = 0;
                GetContent(stream);
                OnUpdate();
            }
        }

        private FileStream GetStream() => new FileStream(Uri.FilePath(), FileMode.Open, FileAccess.Read);

        private byte[] GetFileHash(FileStream stream)
        {
            HashAlgorithm sha1 = HashAlgorithm.Create("SHA1");
            return sha1.ComputeHash(stream);
        }

        private void GetContent(FileStream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
                Content = reader.ReadToEnd();
        }

        public bool Update()
        {
            using (FileStream stream = GetStream())
            {
                byte[] newHash = GetFileHash(stream);
            
                if (!Hash.SequenceEqual(newHash))
                {
                    Hash = newHash;
                    stream.Position = 0;
                    GetContent(stream);
                    OnUpdate();
                    return true;
                }
                else
                    return false;
            }
        }

        protected virtual void OnUpdate() {}
    }
    class ImportedScript : ImportedFile
    {
        public ScriptParseInfo ScriptParseInfo { get; } = new ScriptParseInfo();

        public ImportedScript(Uri uri) : base(uri) {}

        protected override void OnUpdate()
        {
            ScriptParseInfo.Update(Content);
        }
    }

    class DocumentScript
    {
        public Uri Uri { get; }
        public string Content { get; private set; }
        public ScriptParseInfo ScriptParseInfo { get; } = new ScriptParseInfo();

        public DocumentScript(Uri uri)
        {
            Uri = uri;
        }

        public void Update(string content)
        {
            if (Content == content) return;
            Content = content;
            ScriptParseInfo.Update(content);
        }
    }

    public class ScriptParseInfo
    {
        public DeltinScriptParser.RulesetContext Context { get; private set; }
        public List<Diagnostic> StructuralDiagnostics { get; private set; }
        public IToken[] Tokens { get; private set; }

        public ScriptParseInfo() {}

        public ScriptParseInfo(string content)
        {
            Update(content);
        }

        public void Update(string content)
        {
            AntlrInputStream inputStream = new AntlrInputStream(content);

            // Lexer
            DeltinScriptLexer lexer = new DeltinScriptLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            commonTokenStream.Fill();
            Tokens = commonTokenStream.GetTokens().ToArray();
            commonTokenStream.Reset();

            // Parse
            DeltinScriptParser parser = new DeltinScriptParser(commonTokenStream);
            var errorListener = new ErrorListener();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            StructuralDiagnostics = errorListener.Diagnostics;
            Context = parser.ruleset();
        }
    }

    class Importer
    {
        public List<Uri> ImportedFiles { get; } = new List<Uri>();

        public Importer()
        {
        }
    }

    class FileImporter
    {
        public Importer Importer { get; }
        public List<Uri> ImportedInThisScope { get; } = new List<Uri>();
        public FileDiagnostics Diagnostics { get; }

        public FileImporter(FileDiagnostics diagnostics, Importer importer)
        {
            Diagnostics = diagnostics;
            Importer = importer;
        }

        public ImportResult Import(DocRange errorRange, string relativePath, Uri referencePath)
        {
            return new ImportResult(this, errorRange, relativePath, referencePath);
        }
    }

    class ImportResult
    {
        public Uri Uri { get; }
        public bool Successful { get; }
        private string FileName { get; }
        private string FileType { get; }
        public string Directory { get; }

        public ImportResult(FileImporter fileImporter, DocRange importRange, string relativePath, Uri referencingFile)
        {
            FileName = Path.GetFileName(relativePath);
            FileType = Path.GetExtension(relativePath);
            string resultingPath = Extras.CombinePathWithDotNotation(referencingFile.FilePath(), relativePath);
            
            // Syntax error if the filename has invalid characters.
            if (resultingPath == null)
            {
                fileImporter.Diagnostics.Error("File path contains invalid characters.", importRange);
                return;
            }
            Uri = new Uri(resultingPath);
            Directory = Path.GetDirectoryName(resultingPath);

            // Syntax error if the file is importing itself.
            if (Uri == referencingFile)
            {
                fileImporter.Diagnostics.Warning("File is importing itself.", importRange);
                return;
            }

            // Syntax error if the file does not exist.
            if (!System.IO.File.Exists(resultingPath))
            {
                fileImporter.Diagnostics.Error($"The file '{resultingPath}' does not exist.", importRange);
                return;
            }

            // Warning if the file was already imported.
            if (fileImporter.ImportedInThisScope.Contains(Uri))
            {
                fileImporter.Diagnostics.Warning("This file was already imported.", importRange);
                return;
            }

            // Silently fail if another file already imported the file being imported.
            if (fileImporter.Importer.ImportedFiles.Contains(Uri)) return;

            Successful = true;
            fileImporter.ImportedInThisScope.Add(Uri);
            fileImporter.Importer.ImportedFiles.Add(Uri);
        }
    }
}