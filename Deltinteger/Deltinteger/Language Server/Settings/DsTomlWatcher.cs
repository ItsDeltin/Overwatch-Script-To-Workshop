using System;
using System.IO;
using System.Collections.Generic;
using DsTomlSettings = Deltin.Deltinteger.Parse.Settings.DsTomlSettings;

namespace Deltin.Deltinteger.LanguageServer.Settings
{
    using TomlSettings;

    public class DsTomlWatcher : AbstractSettingsHandler<TomlFile<DsTomlSettings>>
    {
        const string projectFileName = "ds.toml";

        public DsTomlWatcher(LanguageServerBuilder builder) : base(builder, "**/" + projectFileName) { }

        public override bool IsMatch(Uri uri) => GetFileName(uri) == projectFileName;
        protected override TomlFile<DsTomlSettings> Create(Uri uri, ITomlDiagnosticReporter reporter) => new TomlFile<DsTomlSettings>(uri, reporter);
        protected override IEnumerable<string> GetFilesInWorkspaceFolder(string workspaceFolder) =>
            Directory.EnumerateFiles(workspaceFolder, projectFileName, SearchOption.TopDirectoryOnly);

        public SourcedSettings<DsTomlSettings> GetProjectSettings(Uri uri)
        {
            var dsToml = GetSettingsFromUri(uri);
            if (dsToml != null)
            {
                return new(dsToml.Uri, dsToml.Settings);
            }
            else
            {
                return new(null, DsTomlSettings.Default);
            }
        }
    }
}