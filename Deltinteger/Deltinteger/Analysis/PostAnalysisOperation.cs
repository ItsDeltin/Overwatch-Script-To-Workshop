using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace DS.Analysis
{
    /// <summary>
    /// Expensive analysis operations that positively affect user experience but are not immediately required should be queued here.
    /// 
    /// For example, getting the name of a type as it is represented in the current context is not useful to the analyzing process but will help the user
    /// recognize a type they have aliased from another file.
    /// </summary>
    class PostAnalysisOperation
    {
        List<Func<IDisposable>> actions = new List<Func<IDisposable>>();


        /// <summary>Adds an operation to the queue.</summary>
        /// <param name="action">The action to execute when analysis completes. The IDisposable created from this action will be linked to the current file.</param>
        /// <returns>An IDisposable which can be used to remove the action from the queue.</returns>
        public IDisposable Add(Func<IDisposable> action)
        {
            actions.Add(action);
            return Disposable.Create(() => actions.Remove(action));
        }


        public IDisposable ExecuteAndReset()
        {
            var result = new CompositeDisposable(actions.Select(action => action()).ToArray());
            actions.Clear();
            return result;
        }
    }
}