using System;
using System.IO;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.Parse;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.LanguageServer.Settings
{
    // Gets the parser settings for the provided uri.
    public interface IParserSettingsResolver
    {
        ParserSettings GetParserSettings(Uri uri);
    }

    // An IParserSettingsResolver implementation that returns the default settings no matter which uri is provided.
    public struct DefaultSettingsResolver : IParserSettingsResolver
    {
        public ParserSettings GetParserSettings(Uri uri) => ParserSettings.Default;
    }

    // Watches ostw module files.
    public class ParserSettingsResolver : AbstractSettingsHandler<GenericSettingFile<ParserSettingsResolver.ModuleSettingFileImport>>, IParserSettingsResolver
    {
        // Watch for changes to files named module.json.
        public ParserSettingsResolver(LanguageServerBuilder builder) : base(builder, "**/module.json") { }

        // Gets the ParserSettings for the specified uri.
        public ParserSettings GetParserSettings(Uri uri)
        {
            var settingsFile = GetSettingsFromUri(uri);
            if (settingsFile == null) return ParserSettings.Default;
            return new ParserSettings()
            {
                DeclarationVersion = settingsFile.Settings.Version
            };
        }

        protected override IEnumerable<string> GetFilesInWorkspaceFolder(string workspaceFolder) =>
            Directory.GetFiles(workspaceFolder, "module.json", SearchOption.AllDirectories);
        public override bool IsMatch(Uri uri) => GetFileName(uri) == "module.json";
        protected override GenericSettingFile<ModuleSettingFileImport> Create(Uri uri) => new GenericSettingFile<ModuleSettingFileImport>(uri);


        public struct ModuleSettingFileImport
        {
            [JsonProperty("version")]
            public DeclarationVersion Version;
        }
    }
}