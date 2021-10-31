using System;
using System.Collections.Generic;
using DS.Analysis.Structure.Utility;

namespace DS.Analysis.Structure
{
    class DeclarationObserver : IDisposable
    {
        readonly List<IDeclarationListener> _listeners = new List<IDeclarationListener>();

        public IDisposable Subscribe(IDeclarationListener observer)
        {
            _listeners.Add(observer);
            return new DeclarationUnsubscriber(this, observer);
        }

        public void Dispose()
        {
            foreach (var listener in _listeners)
                listener.UnlinkDeclaration();
        }

        class DeclarationUnsubscriber : IDisposable
        {
            readonly DeclarationObserver _observer;
            readonly IDeclarationListener _listener;

            public DeclarationUnsubscriber(DeclarationObserver observer, IDeclarationListener listener)
            {
                _observer = observer;
                _listener = listener;
            }

            public void Dispose()
            {
                if (!_observer._listeners.Remove(_listener))
                    throw new Exception("Not listening to declaration");
            }
        }
    }
}