using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    class RecursionCheckComponent : IComponent
    {
        readonly HashSet<CallInfo> _checkForRecursion = new HashSet<CallInfo>();
        DeltinScript _deltinScript;

        public void AddCheck(CallInfo callInfo) => _checkForRecursion.Add(callInfo);
        public void Init(DeltinScript deltinScript)
        {
            _deltinScript = deltinScript;
            deltinScript.StagedInitiation.On(InitiationStage.PostContent, CheckRecursion);
        }

        void CheckRecursion()
        {
            foreach (var callInfo in _checkForRecursion)
                callInfo.CheckRecursion(_deltinScript);
        }
    }
}