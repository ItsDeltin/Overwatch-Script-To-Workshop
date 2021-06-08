using System;
using System.Linq;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    class CallInfoVisitor
    {
        public static void Visit(CallInfo callInfo, Action<CallInfo> onCallInfo) => new CallInfoVisitor(onCallInfo).Visit(callInfo);
        public static IEnumerable<RestrictedCallType> CollectRestrictedCalls(CallInfo callInfo)
        {
            var restrictedCalls = new HashSet<RestrictedCallType>();
            Visit(callInfo, c => {
                foreach (var restrictedCall in c.RestrictedCalls)
                    restrictedCalls.Add(restrictedCall.CallType);
            });
            return restrictedCalls;
        }

        readonly HashSet<CallInfo> _visited = new HashSet<CallInfo>();
        readonly Action<CallInfo> _callback;

        private CallInfoVisitor(Action<CallInfo> callback) => _callback = callback;

        public void Visit(CallInfo callInfo)
        {
            // Do not visit again.
            if (!_visited.Add(callInfo))
                return;
            
            _callback(callInfo);

            // Check sub calls.
            foreach (var subcall in callInfo.Calls)
                Visit(subcall.CallInfo);
        }
    }
}