using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public class FileGetter
    {
        private DocumentHandler DocumentHandler { get; }
        // Importing scripts not being edited.
        private List<ImportedFile> ImportedFiles = new List<ImportedFile>();

        public FileGetter(DocumentHandler documentHandler)
        {
            DocumentHandler = documentHandler;
        }

        public Document GetScript(Uri uri)
        {
            // Get the content of the script being obtained.
            Document doc = DocumentHandler?.Documents.FirstOrDefault(td => td.Uri.Compare(uri));
            if (doc != null) return doc;

            ImportedScript importedFile = GetImportedFile(uri);
            importedFile.Update();
            return importedFile.Document;
        }

        public ImportedScript GetImportedFile(Uri uri)
        {
            foreach (ImportedFile importedFile in ImportedFiles)
                if (importedFile.Uri == uri)
                    return (ImportedScript)importedFile;
            var newImportedFile = new ImportedScript(uri);
            ImportedFiles.Add(newImportedFile);
            return newImportedFile;
        }

        public ImportedBlendFile GetBlendFile(Uri uri)
        {
            if (TryGetExisting(uri, out ImportedBlendFile script)) return script;
            return AddFile(new ImportedBlendFile(uri));
        }

        private bool TryGetExisting<T>(Uri uri, out T script) where T: ImportedFile
        {
            foreach (ImportedFile file in ImportedFiles)
                if (file.Uri == uri)
                {
                    script = (T)file;
                    return true;
                }
            script = default(T);
            return false;
        }

        private T AddFile<T>(T file) where T: ImportedFile
        {
            ImportedFiles.Add(file);
            return file;
        }
    }
}