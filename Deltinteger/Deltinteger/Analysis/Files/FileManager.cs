using System;
using System.Linq;
using System.Collections.Generic;

namespace DS.Analysis.Files
{
    class FileManager
    {
        readonly List<File> files = new List<File>();
        readonly List<FileDependency> dependencies = new List<FileDependency>();

        public IDisposable Depend(string path, IFileDependent dependent)
        {
            var dependency = new FileDependency(path, dependent);
            dependencies.Add(dependency);
            return new UnlinkDependency(this, dependency);
        }

        bool TryGetFile(string path, out File file)
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

        public bool TryLoad(string path, out File file)
        {

        }

        public void Unload()
        {

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

                // TODO: unload file if references == 0 and external
            }
        }
    }
}