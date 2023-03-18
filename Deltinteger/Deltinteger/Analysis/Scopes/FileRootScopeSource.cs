using System;
using System.IO;
using DS.Analysis.Core;
using DS.Analysis.Files;

namespace DS.Analysis.Scopes
{
    /// <summary>
    /// Imports the entire root scope of another file into the current scope.
    /// </summary>
    class FileRootScopeSource : IScopeSource, IFileDependent, IDisposable
    {
        // IScopeSource
        public ScopedElement[] Elements { get; private set; }

        readonly SingleNode node;
        readonly IDisposable fileSubscription;
        readonly string path;
        readonly IFileImportErrorHandler errorHandler;
        IDisposable scopeSubscription;

        ScriptFile file;

        public FileRootScopeSource(DSAnalysis analysis, string path, IFileImportErrorHandler errorHandler)
        {
            node = analysis.SingleNode($"File scope [{path}]", () =>
            {
                Elements = file == null ? new ScopedElement[0] : file.RootScopeSource.Elements;
                node.MakeDependentsStale();
            });
            this.path = path;
            this.errorHandler = errorHandler;
            fileSubscription = analysis.FileManager.Depend(path, this);
        }

        // IFileDependent
        void IFileDependent.SetFile(ScriptFile file, Exception exception)
        {
            this.file = file;
            scopeSubscription?.Dispose();
            scopeSubscription = null;

            // File does not exist.
            if (file == null)
            {
                Elements = new ScopedElement[0];
                DispatchError(exception);
                node.MakeDependentsStale();
                return;
            }

            scopeSubscription = node.DependOn(file.RootScopeSource);
            errorHandler.Success();
        }

        // IDisposable
        public void Dispose()
        {
            fileSubscription.Dispose();
            scopeSubscription?.Dispose();
        }

        void DispatchError(Exception exception)
        {
            if (exception is ArgumentException ||
                exception is NotSupportedException)
                errorHandler.Error($"Invalid path format: '{path}'");

            else if (exception is PathTooLongException)
                errorHandler.Error($"Path is too long: '{path}'");

            else if (exception is DirectoryNotFoundException)
                errorHandler.Error($"Directory not found: '{path}'");

            else if (exception is FileNotFoundException)
                errorHandler.Error($"File not found: '{path}'");

            else if (exception is UnauthorizedAccessException ||
                     exception is System.Security.SecurityException)
                errorHandler.Error($"Unauthorized: '{path}': {exception.Message}");

            else if (exception is IOException)
                errorHandler.Error($"IO error: '{path}': {exception.Message}");

            else
                throw exception;
        }

        // IScopeSource
        public IDisposable AddDependent(IDependent dependent) => node.AddDependent(dependent);
    }

    interface IFileImportErrorHandler
    {
        void Success();
        void Error(string message);
    }
}