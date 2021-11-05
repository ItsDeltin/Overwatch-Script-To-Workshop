using System;
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
        IDisposable scopeSubscription;
        ScopeSourceChange currentValue;

        public FileRootScopeSource(DeltinScriptAnalysis analysis, string path)
        {
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
                return;
            }

            scopeSubscription = file.RootScopeSource.Subscribe(value => {
                currentValue = value;
                observers.Set(value);
            });
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
    }
}