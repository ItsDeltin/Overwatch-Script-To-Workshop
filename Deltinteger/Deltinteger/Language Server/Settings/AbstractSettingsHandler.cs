using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.LanguageServer.Settings
{
    using TomlSettings;

    public abstract class AbstractSettingsHandler<T> : IDidChangeWatchedFilesPatternHandler where T : class, ISettingFile
    {
        readonly ServerWorkspace _workspace;
        readonly List<T> _settingFiles = new List<T>();
        readonly ITomlDiagnosticReporter _diagnosticReporter;

        public AbstractSettingsHandler(LanguageServerBuilder builder, string glob)
        {
            _workspace = builder.Server.Workspace;
            _diagnosticReporter = builder.TomlDiagnosticsReporter;
            builder.FileHandlerBuilder.AddWatcher(glob, this);
        }

        public void GetInitialFiles()
        {
            // Get the initial module setting files.
            // Any added later is handled by the 'Handle' function.
            foreach (var folder in _workspace.WorkspaceFolders)
            {
                try
                {
                    // Get the module files in the workspace.
                    foreach (var file in GetFilesInWorkspaceFolder(folder.Uri.ToUri().LocalPath))
                        Add(Create(new Uri(file), _diagnosticReporter));
                }
                // This may occur if a folder is added to a workspace in vscode, but that folder no longer exists.
                catch (DirectoryNotFoundException) { }
            }
        }

        void IDidChangeWatchedFilesPatternHandler.Handle(Uri uri, FileChangeType kind)
        {
            switch (kind)
            {
                case FileChangeType.Created: Add(Create(uri, _diagnosticReporter)); break;
                case FileChangeType.Deleted: Remove(uri); break;
                case FileChangeType.Changed:
                    bool fileFound = false;
                    foreach (var moduleFile in _settingFiles)
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

        void Add(T file) => _settingFiles.Add(file);

        void Remove(Uri uri)
        {
            T removing = null;

            // Find the module file with the matching uri in the _moduleSettingsFiles list. 
            foreach (var moduleFile in _settingFiles)
                if (uri.Compare(moduleFile.Uri))
                {
                    removing = moduleFile;
                    break;
                }

            if (!_settingFiles.Remove(removing))
                Debug.Fail("Tried to remove non-existent module settings file: '" + uri.ToString() + "'");
        }

        // Gets the setting files in the provided workspace folder.
        protected abstract IEnumerable<string> GetFilesInWorkspaceFolder(string workspaceFolder);
        // Creates a T from a Uri
        protected abstract T Create(Uri uri, ITomlDiagnosticReporter reporter);
        // Determines if a modified Uri in the project should be handled by this AbstractSettingsHandler.
        public abstract bool IsMatch(Uri uri);

        public T GetSettingsFromUri(Uri uri)
        {
            foreach (var file in _settingFiles)
            {
                var directory = new Uri(file.Uri, ".");
                if (directory.IsBaseOf(uri))
                    return file;
            }
            return null;
        }

        protected static string GetFileName(Uri uri) => Path.GetFileName(uri.LocalPath)?.ToLower();
    }

    public interface ISettingFile
    {
        Uri Uri { get; }
        void Update();
    }
}