using System;
using System.IO;
using System.Collections.Generic;
using ProjectSettings = Deltin.Deltinteger.Parse.Settings.ProjectSettings;

namespace Deltin.Deltinteger.LanguageServer.Settings
{
    public class ProjectSettingsWatcher : AbstractSettingsHandler<GenericSettingFile<ProjectSettings>>
    {
        const string projectFileName = "deltinscript.json";

        public ProjectSettingsWatcher(LanguageServerBuilder builder) : base(builder, "**/" + projectFileName) { }

        public override bool IsMatch(Uri uri) => GetFileName(uri) == projectFileName;
        protected override GenericSettingFile<ProjectSettings> Create(Uri uri) => new GenericSettingFile<ProjectSettings>(uri);
        protected override IEnumerable<string> GetFilesInWorkspaceFolder(string workspaceFolder) =>
            Directory.EnumerateFiles(workspaceFolder, projectFileName, SearchOption.TopDirectoryOnly);
        
        public ProjectSettings GetProjectSettings(Uri uri)
        {
            var settingsFile = GetSettingsFromUri(uri);
            if (settingsFile != null)
                return settingsFile.Settings;

            return ProjectSettings.Default;
        }
    }
}