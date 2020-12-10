using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Deltin.Deltinteger.Compiler.Parse;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.LanguageServer
{
    public interface IParserSettingsResolver
    {
        ParserSettings GetParserSettings(Uri uri);
    }

    public struct DefaultSettingsResolver : IParserSettingsResolver
    {
        public ParserSettings GetParserSettings(Uri uri) => ParserSettings.Default;
    }

    public class ParserSettingsResolver : IParserSettingsResolver, IDidChangeWatchedFilesPatternHandler
    {
        private readonly DeltintegerLanguageServer _server;
        private readonly List<ModuleSettingFile> _moduleSettingsFiles = new List<ModuleSettingFile>();

        public ParserSettingsResolver(LanguageServerBuilder builder)
        {
            builder.FileHandlerBuilder.AddWatcher("**/module.json", this);
            _server = builder.Server;
        }

        public void GetModuleFiles()
        {
            // Get module setting files.
            foreach(var folder in _server.Workspace.WorkspaceFolders)
            {
                var moduleFiles = Directory.GetFiles(folder.Uri.ToUri().LocalPath, "module.json", SearchOption.AllDirectories);
                foreach (var file in moduleFiles)
                    Add(new Uri(file));
            }
        }

        public ParserSettings GetParserSettings(Uri uri)
        {
            foreach (var moduleFile in _moduleSettingsFiles)
            {
                var directory = new Uri(moduleFile.Uri, ".");
                if (directory.IsBaseOf(uri))
                    return moduleFile.GetParserSettings();
            }
            return ParserSettings.Default;
        }

        public void Handle(Uri uri, FileChangeType kind)
        {
            switch(kind)
            {
                case FileChangeType.Created: Add(uri); break;
                case FileChangeType.Deleted: Remove(uri); break;
                case FileChangeType.Changed:
                    bool fileFound = false;
                    foreach (var moduleFile in _moduleSettingsFiles)
                        if (uri.Compare(moduleFile.Uri))
                        {
                            fileFound = true;
                            moduleFile.Update();
                            break;
                        }
                    Debug.Assert(fileFound, "Tried to update non-existent module settings file: '" + uri.ToString() + "'");
                    break;
            }
        }

        public bool IsMatch(Uri uri) => Path.GetFileName(uri.LocalPath)?.ToLower() == "module.json";

        private void Add(Uri uri) => _moduleSettingsFiles.Add(new ModuleSettingFile(uri));

        private void Remove(Uri uri)
        {
            ModuleSettingFile removing = null;

            foreach (var moduleFile in _moduleSettingsFiles)
                if (uri.Compare(moduleFile.Uri))
                {
                    removing = moduleFile;
                    break;
                }
            
            if (_moduleSettingsFiles != null)
                _moduleSettingsFiles.Remove(removing);
            else
                Debug.Fail("Tried to remove non-existent module settings file: '" + uri.ToString() + "'");
        }

        class ModuleSettingFile
        {
            public Uri Uri { get; }
            public ModuleSettingFileImport Settings { get; private set; }

            public ModuleSettingFile(Uri uri)
            {
                Uri = uri;
                Update();
            }

            public void Update()
            {
                try
                {
                    string content = File.ReadAllText(Uri.LocalPath);
                    Settings = JsonConvert.DeserializeObject<ModuleSettingFileImport>(content);
                }
                catch(Exception ex)
                {
                    // TODO: add diagnostics for user
                }
            }

            public ParserSettings GetParserSettings() => new ParserSettings() {
                DeclarationVersion = Settings.Version
            };

            public struct ModuleSettingFileImport
            {
                [JsonProperty("version")]
                public DeclarationVersion Version;
            }
        }
    }
}