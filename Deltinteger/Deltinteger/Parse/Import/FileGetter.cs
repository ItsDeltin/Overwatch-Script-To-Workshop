using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.LanguageServer.Settings;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public class FileGetter
    {
        private DocumentHandler DocumentHandler { get; }
        // Importing scripts not being edited.
        private List<ImportedFile> ImportedFiles = new List<ImportedFile>();
        private readonly IParserSettingsResolver _settingsResolver;

        public FileGetter(DocumentHandler documentHandler, IParserSettingsResolver settingsResolver)
        {
            DocumentHandler = documentHandler;
            _settingsResolver = settingsResolver;
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
            foreach (ImportedScript importedFile in ImportedFiles)
                if (importedFile.Uri == uri)
                    return importedFile;
            var newImportedFile = new ImportedScript(uri, _settingsResolver);
            ImportedFiles.Add(newImportedFile);
            return newImportedFile;
        }
    }
}