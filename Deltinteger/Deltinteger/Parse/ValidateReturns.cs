using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public class ReturnTracker
    {
        public IReadOnlyList<ReturnAction> Returns => _returns;
        public bool IsMultiplePaths => _returns.Count > 1;
        public CodeType InferredType => _returns.Count == 0 ? null : _returns[0].ReturningValue.Type();
        public bool ReturnsValue { get; private set; }

        readonly List<ReturnAction> _returns = new List<ReturnAction>();
        public void Add(ReturnAction returnHandler)
        {
            _returns.Add(returnHandler);
            ReturnsValue = ReturnsValue || returnHandler.ReturningValue != null;
        }
    }
}