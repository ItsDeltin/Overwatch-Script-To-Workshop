using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Lobby;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Json;
using Newtonsoft.Json.Linq;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    class Importer
    {
        public List<Uri> ImportedFiles { get; } = new List<Uri>();
        public List<ScriptFile> ScriptFiles { get; } = new List<ScriptFile>();
        public List<ImportedScript> OWFiles { get; } = new List<ImportedScript>();
        public JObject MergedLobbySettings { get; private set; }
        private readonly Diagnostics _diagnostics;
        private readonly FileGetter _fileGetter;
        private readonly DeltinScript _deltinScript;

        public Importer(DeltinScript deltinScript, FileGetter fileGetter, Uri initial)
        {
            _deltinScript = deltinScript;
            _diagnostics = deltinScript.Diagnostics;
            _fileGetter = fileGetter;
            ImportedFiles.Add(initial);
        }

        public void CollectScriptFiles(ScriptFile scriptFile)
        {
            ScriptFiles.Add(scriptFile);

            FileImporter importer = new FileImporter(scriptFile.Diagnostics, this);

            // Get the imported files.
            if (scriptFile.Context.import_file() != null)
            {
                foreach (var importFileContext in scriptFile.Context.import_file())
                {
                    string directory = GetImportedFile(scriptFile, importer, importFileContext);
                    if (Directory.Exists(directory))
                        AddImportCompletion(scriptFile, directory, DocRange.GetRange(importFileContext.STRINGLITERAL()));
                }
            }
        }


        public static void AddImportCompletion(ScriptFile script, string directory, DocRange range)
        {
            List<CompletionItem> completionItems = new List<CompletionItem>();
            var directories = Directory.GetDirectories(directory);
            var files = Directory.GetFiles(directory);

            completionItems.Add(new CompletionItem() {
                Label = "../",
                Detail = Directory.GetParent(directory).FullName,
                Kind = CompletionItemKind.Folder
            });

            foreach (var dir in directories)
                completionItems.Add(new CompletionItem() {
                    Label = Path.GetFileName(dir),
                    Detail = dir,
                    Kind = CompletionItemKind.Folder
                });
            
            foreach (var file in files)
                completionItems.Add(new CompletionItem() {
                    Label = Path.GetFileName(file),
                    Detail = file,
                    Kind = CompletionItemKind.File
                });
            
            script.AddCompletionRange(new CompletionRange(completionItems.ToArray(), range, CompletionRangeKind.ClearRest));
        }

        string GetImportedFile(ScriptFile script, FileImporter importer, DeltinScriptParser.Import_fileContext importFileContext)
        {
            // If the file being imported is being imported as an object, get the variable name.
            string variableName = null;
            if (importFileContext.AS() != null)
            {
                // Syntax error if there is an 'as' keyword but no variable name.
                if (importFileContext.name == null)
                    script.Diagnostics.Error("Expected variable name.", DocRange.GetRange(importFileContext.AS()));
                // Get the variable name.
                else
                    variableName = importFileContext.name.Text;
            }

            DocRange stringRange = DocRange.GetRange(importFileContext.STRINGLITERAL());

            ImportResult importResult = importer.Import(
                stringRange,
                Extras.RemoveQuotes(importFileContext.STRINGLITERAL().GetText()),
                script.Uri
            );
            if (!importResult.SuccessfulReference) return importResult.Directory;

            // Add hover and definition info.
            script.AddDefinitionLink(stringRange, new Location(importResult.Uri, DocRange.Zero));
            script.AddHover(stringRange, importResult.FilePath);

            if (importResult.ShouldImport)
            {
                // Import the file if it should be imported.
                if (variableName == null)
                    switch (importResult.FileType)
                    {
                        // Get script file.
                        case ".del":
                        case ".ostw":
                        case ".workshop":
                            ScriptFile importedScript = new ScriptFile(_diagnostics, importResult.Uri, _fileGetter.GetScript(importResult.Uri));
                            CollectScriptFiles(importedScript);
                            break;
                        case ".ow":
                            ImportedScript owScript = _fileGetter.GetImportedFile(importResult.Uri);
                            OWFiles.Add(owScript);
                            break;
                        
                        // Get lobby settings.
                        case ".json":
                            JObject lobbySettings = null;

                            // Make sure the json is in the correct format.
                            try
                            {
                                ImportedScript file = _fileGetter.GetImportedFile(importResult.Uri);
                                file.Update();

                                // Convert the json to a jobject.
                                lobbySettings = JObject.Parse(file.Content);

                                // An exception will be thrown if the jobject cannot be converted to a Ruleset.
                                lobbySettings.ToObject(typeof(Ruleset));

                                if (!Ruleset.Validate(lobbySettings, script.Diagnostics, stringRange)) break;
                            }
                            catch
                            {
                                // Error if the json failed to parse.
                                script.Diagnostics.Error("Failed to parse the settings file.", stringRange);
                                break;
                            }

                            // If no lobby settings were imported yet, set MergedLobbySettings to the jobject.
                            if (MergedLobbySettings == null) MergedLobbySettings = lobbySettings;
                            else
                            {
                                // Otherwise, merge current lobby settings.
                                lobbySettings.Merge(MergedLobbySettings, new JsonMergeSettings {
                                    MergeArrayHandling = MergeArrayHandling.Union,
                                    MergeNullValueHandling = MergeNullValueHandling.Ignore
                                });
                                MergedLobbySettings = lobbySettings;
                            }
                            break;
                    }
                else
                    switch (importResult.FileType)
                    {
                        case ".json":
                            ImportedScript file = _fileGetter.GetImportedFile(importResult.Uri);
                            file.Update();

                            JObject jsonData = JObject.Parse(file.Content);
                            InternalVar jsonVar = new InternalVar(importFileContext.name.Text);
                            jsonVar.CodeType = new JsonType(jsonData);

                            if (((JsonType)jsonVar.CodeType).ContainsDeepArrays())
                                script.Diagnostics.Error("JSON Arrays cannot include objects or arrays.", stringRange);

                            _deltinScript.RulesetScope.AddVariable(jsonVar, script.Diagnostics, DocRange.GetRange(importFileContext.name));
                            _deltinScript.DefaultIndexAssigner.Add(jsonVar, new V_Null());                            
                            break;
                    }

            }
            return importResult.Directory;
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