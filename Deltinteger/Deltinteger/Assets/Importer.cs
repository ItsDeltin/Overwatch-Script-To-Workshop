using System.Collections.Generic;
using System.IO;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    class Importer
    {
        public string ResultingPath { get; }
        public string FileName { get; }
        public string FileType { get; }
        public bool AlreadyImported { get; }

        public Importer(Diagnostics diagnostics, List<string> importedFiles, string file, string referencePath, Location location)
        {
            FileName = Path.GetFileName(file);
            FileType = Path.GetExtension(file);
            ResultingPath = Extras.CombinePathWithDotNotation(referencePath, file);
            
            // Syntax error if the filename has invalid characters.
            if (ResultingPath == null)
                throw SyntaxErrorException.InvalidImportPathChars(file, location);

            // Syntax error if the file is importing itself.
            if (referencePath == ResultingPath)
                throw SyntaxErrorException.SelfImport(location);

            // Syntax error if the file does not exist.
            if (!System.IO.File.Exists(ResultingPath))
                throw SyntaxErrorException.ImportFileNotFound(ResultingPath, location);

            // Warning if the file was already imported.
            if (importedFiles.Contains(ResultingPath))
            {
                diagnostics.Warning(string.Format(SyntaxErrorException.alreadyImported, FileName), location);
                AlreadyImported = true;
            }
            else AlreadyImported = false;
        }

        public string GetFile()
        {
            return File.ReadAllText(ResultingPath);
        }
    }
}