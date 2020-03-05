using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Antlr4.Runtime.Tree;

namespace Deltin.Deltinteger.Parse
{
    class Importer
    {
        public List<Uri> ImportedFiles { get; } = new List<Uri>();

        public Importer(Uri initial)
        {
            ImportedFiles.Add(initial);
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
        public bool SuccessfulReference { get; }
        public bool ShouldImport { get; }
        public string Directory { get; }
        public string FilePath { get; }
        public string FileType { get; }

        public ImportResult(FileImporter fileImporter, DocRange importRange, string relativePath, Uri referencingFile)
        {
            string resultingPath = Extras.CombinePathWithDotNotation(referencingFile.FilePath(), relativePath);
            
            // Syntax error if the filename has invalid characters.
            if (resultingPath == null)
            {
                fileImporter.Diagnostics.Error("File path contains invalid characters.", importRange);
                return;
            }
            Uri = Extras.Definition(resultingPath);
            Directory = Path.GetDirectoryName(resultingPath);
            FilePath = resultingPath;
            FileType = Path.GetExtension(FilePath).ToLower();

            // Syntax error if the file does not exist.
            if (!System.IO.File.Exists(resultingPath))
            {
                fileImporter.Diagnostics.Error($"The file '{resultingPath}' does not exist.", importRange);
                return;
            }

            // Syntax error if the file is importing itself.
            if (Uri.Compare(referencingFile))
            {
                fileImporter.Diagnostics.Warning("File is importing itself.", importRange);
                return;
            }

            SuccessfulReference = true;

            // Warning if the file was already imported.
            if (fileImporter.ImportedInThisScope.Any(u => u.Compare(Uri)))
            {
                fileImporter.Diagnostics.Warning("This file was already imported.", importRange);
                return;
            }

            // Silently fail if another file already imported the file being imported.
            if (fileImporter.Importer.ImportedFiles.Any(u => u.Compare(Uri))) return;

            ShouldImport = true;
            fileImporter.ImportedInThisScope.Add(Uri);
            fileImporter.Importer.ImportedFiles.Add(Uri);
        }
    }
}