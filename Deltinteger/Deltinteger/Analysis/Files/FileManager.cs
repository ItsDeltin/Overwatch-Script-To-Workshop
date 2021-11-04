using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace DS.Analysis.Files
{
    class FileManager
    {
        readonly List<ScriptFile> files = new List<ScriptFile>();
        readonly List<FileDependency> dependencies = new List<FileDependency>();

        readonly DeltinScriptAnalysis analysis;


        public FileManager(DeltinScriptAnalysis analysis)
        {
            this.analysis = analysis;
        }


        public IDisposable Depend(string path, IFileDependent dependent)
        {
            // Create dependency.
            var dependency = new FileDependency(path, dependent);
            dependencies.Add(dependency);

            // Set the dependent's initial value.
            if (TryGetFile(path, out ScriptFile file))
                dependent.SetFile(file, null);
            else
                LoadExternal(path);

            return new UnlinkDependency(this, dependency);
        }

        void NotifyDependencies(string path, ScriptFile file, Exception ex)
        {
            foreach (var dep in dependencies.Where(dep => dep.Path == path))
                dep.Dependent.SetFile(file, ex);
        }

        bool TryGetFile(string path, out ScriptFile file)
        {
            foreach (var f in files)
                if (f.Path == path)
                {
                    file = f;
                    return true;
                }
            
            file = null;
            return false;
        }

        public void LoadExternal(string path)
        {
            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                NotifyDependencies(path, null, ex);
                return;
            }

            // Create script
            ScriptFile newFile = new ScriptFile(path, true, analysis);
            files.Add(newFile);

            // Parse script
            newFile.SetFromString(text);
            newFile.GetStructure();

            NotifyDependencies(path, newFile, null);
        }

        void Unload(ScriptFile file)
        {
            if (!files.Remove(file))
                throw new Exception("file is not loaded");
            file.Unlink();
            NotifyDependencies(file.Path, null, null);
        }


        class FileDependency
        {
            public string Path;
            public IFileDependent Dependent;

            public FileDependency(string path, IFileDependent dependent)
            {
                Path = path;
                Dependent = dependent;
            }
        }


        class UnlinkDependency : IDisposable
        {
            readonly FileManager fileManager;
            readonly FileDependency fileDependency;

            public UnlinkDependency(FileManager fileManager, FileDependency fileDependency)
            {
                this.fileManager = fileManager;
                this.fileDependency = fileDependency;
            }

            public void Dispose()
            {
                // Remove dependency from the list.
                fileManager.dependencies.Remove(fileDependency);

                // Unload file if there aren't any more dependencies with the same path.
                if (fileManager.dependencies.Count(dep => dep.Path == fileDependency.Path) == 0)
                {
                    // Find the ScriptFile with the matching path.
                    ScriptFile file = fileManager.files.FirstOrDefault(dep => dep.Path == fileDependency.Path);

                    // Unload if the ScriptFile exists and the ScriptFile is marked as external.
                    if (file != null && file.IsExternal)
                        fileManager.Unload(file);
                }
            }
        }
    }
}