using System;
using System.IO;
using DS.Analysis.Files;
using DS.Analysis.Utility;

namespace DS.Analysis.Scopes.Import
{
    /// <summary>
    /// Imports the entire root scope of another file into the current scope.
    /// </summary>
    class FileRootScopeSource : IScopeSource, IFileDependent, IDisposable
    {
        readonly ObserverCollection<ScopeSourceChange> observers = new ObserverCollection<ScopeSourceChange>();
        readonly IDisposable fileSubscription;
        readonly string path;
        readonly IFileImportErrorHandler errorHandler;
        IDisposable scopeSubscription;
        ScopeSourceChange currentValue;

        public FileRootScopeSource(DeltinScriptAnalysis analysis, string path, IFileImportErrorHandler errorHandler)
        {
            this.path = path;
            this.errorHandler = errorHandler;
            fileSubscription = analysis.FileManager.Depend(path, this);
        }

        void IFileDependent.SetFile(ScriptFile file, Exception exception)
        {
            scopeSubscription?.Dispose();
            scopeSubscription = null;
            currentValue = ScopeSourceChange.Empty;

            // File does not exist.
            if (file == null)
            {
                observers.Set(ScopeSourceChange.Empty);
                DispatchError(exception);
                return;
            }

            scopeSubscription = file.RootScopeSource.Subscribe(value => {
                currentValue = value;
                observers.Set(value);
            });
            errorHandler.Success();
        }

        public IDisposable Subscribe(IObserver<ScopeSourceChange> observer)
        {
            observer.OnNext(currentValue);
            return observers.Add(observer);
        }

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
    }

    interface IFileImportErrorHandler
    {
        void Success();
        void Error(string message);
    }
}