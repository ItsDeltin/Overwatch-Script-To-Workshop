using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    class Importer
    {
        public string ResultingPath { get; }
        public string FileName { get; }
        public string FileType { get; }
        public bool AlreadyImported { get; }
        public ImportedFile FileData { get; }

        public Importer(FileDiagnostics diagnostics, List<string> importedFiles, string file, string referencePath, Location location)
        {
            FileName = Path.GetFileName(file);
            FileType = Path.GetExtension(file);
            ResultingPath = Extras.CombinePathWithDotNotation(referencePath, file);
            
            // Syntax error if the filename has invalid characters.
            // if (ResultingPath == null)
                // throw SyntaxErrorException.InvalidImportPathChars(file, location);

            // Syntax error if the file is importing itself.
            // if (referencePath == ResultingPath)
                // throw SyntaxErrorException.SelfImport(location);

            // Syntax error if the file does not exist.
            // if (!System.IO.File.Exists(ResultingPath))
                // throw SyntaxErrorException.ImportFileNotFound(ResultingPath, location);

            // Warning if the file was already imported.
            if (importedFiles.Contains(ResultingPath))
            {
                // diagnostics.Warning(string.Format(SyntaxErrorException.alreadyImported, FileName), location.range);
                AlreadyImported = true;
            }
            else AlreadyImported = false;

            FileData = ImportedFile.GetImportedFile(ResultingPath);
        }
    }

    class ImportedFile
    {
        public string File { get; }
        public string Content { get; private set; }
        private byte[] Hash { get; set; }
        public object Cache { get; set; }

        protected ImportedFile(string file)
        {
            File = file;
            
            using (FileStream stream = GetStream())
            {
                Hash = GetFileHash(stream);
                stream.Position = 0;
                GetContent(stream);
            }

            ImportedFiles.Add(this);
        }

        private FileStream GetStream() => new FileStream(File, FileMode.Open, FileAccess.Read);

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
                    return true;
                }
                else
                    return false;
            }
        }

        private static List<ImportedFile> ImportedFiles = new List<ImportedFile>();

        public static ImportedFile GetImportedFile(string file)
        {
            foreach (ImportedFile importedFile in ImportedFiles)
            if (importedFile.File == file)
                return importedFile;
            return new ImportedFile(file);
        }
    }
}