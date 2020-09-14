using System;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse.Lambda
{
    
    public interface IBridgeInvocable
    {
        void WasInvoked();
        void OnInvoke(Action onInvoke);
    }

    public class SubLambdaInvoke : IBridgeInvocable
    {
        public bool Invoked { get; private set; }
        public List<Action> Actions { get; } = new List<Action>();
        
        public void WasInvoked() => Invoked = true;
        public void OnInvoke(Action onInvoke) => Actions.Add(onInvoke);
    }
}