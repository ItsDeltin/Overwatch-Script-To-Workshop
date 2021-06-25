using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse.Functions.Builder
{
    public class RecursiveVariableTracker
    {
        readonly ActionSet _actionSet;
        readonly RecursiveVariableTracker _parent;
        readonly List<IGettable> _popVariables = new List<IGettable>();

        public RecursiveVariableTracker(ActionSet actionSet, RecursiveVariableTracker parent)
        {
            _actionSet = actionSet;
            _parent = parent;
        }

        public void Add(IGettable gettable) => _popVariables.Add(gettable);

        public void PopAll()
        {
            // The order doesn't really matter, it will be functionally the same. We can just go through each parent popping the values.
            // This is just for cosmetic reasons
            var order = new Stack<RecursiveVariableTracker>();
            var current = this;
            while (current != null)
            {
                order.Push(current);
                current = current._parent;
            }

            while (order.TryPop(out var tracker))
                foreach (var pop in tracker._popVariables)
                    pop.Pop(_actionSet);
        }

        public void PopLocal()
        {
            foreach (var pop in _popVariables)
                pop.Pop(_actionSet);
        }
    }
}