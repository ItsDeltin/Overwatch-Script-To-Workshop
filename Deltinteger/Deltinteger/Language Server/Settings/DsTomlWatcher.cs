using System;
using System.IO;
using System.Collections.Generic;
using ProjectSettings = Deltin.Deltinteger.Parse.Settings.ProjectSettings;

namespace Deltin.Deltinteger.LanguageServer.Settings
{
    using TomlSettings;

    public class DsTomlWatcher : AbstractSettingsHandler<TomlFile<ProjectSettings>>
    {
        const string projectFileName = "ds.toml";

        public DsTomlWatcher(LanguageServerBuilder builder) : base(builder, "**/" + projectFileName) { }

        public override bool IsMatch(Uri uri) => GetFileName(uri) == projectFileName;
        protected override TomlFile<ProjectSettings> Create(Uri uri, ITomlDiagnosticReporter reporter) => new TomlFile<ProjectSettings>(uri, reporter);
        protected override IEnumerable<string> GetFilesInWorkspaceFolder(string workspaceFolder) =>
            Directory.EnumerateFiles(workspaceFolder, projectFileName, SearchOption.TopDirectoryOnly);

        public ProjectSettings GetProjectSettings(Uri uri)
        {
            return GetSettingsFromUri(uri)?.Settings ?? ProjectSettings.Default;
        }
    }
}