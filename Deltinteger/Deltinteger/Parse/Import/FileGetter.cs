using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;

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
            // Get the content of the script being obtained.
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

        public ImportedScript GetImportedFile(Uri uri)
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
}