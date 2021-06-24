using System;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    class ApplyBlock
    {
        private readonly List<IOnBlockApplied> _onBlockApplied = new List<IOnBlockApplied>();
        private bool _wasApplied = false;

        public void OnBlockApply(IOnBlockApplied onBlockApplied)
        {
            if (_wasApplied)
                onBlockApplied.Applied();
            else
                _onBlockApplied.Add(onBlockApplied);
        }

        public void Apply()
        {
            if (_wasApplied) throw new Exception("Already applied.");
            _wasApplied = true;

            foreach (var onApply in _onBlockApplied)
                onApply.Applied();
        }
    }
}