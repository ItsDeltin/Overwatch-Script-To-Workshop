using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using RuleEvent = Deltin.Deltinteger.Elements.RuleEvent;

namespace Deltin.Deltinteger.Parse
{
    // Tracks calls executed in a script.
    public class CallInfo : IRestrictedCallHandler, IGetRestrictedCallTypes
    {
        public IRecursiveCallHandler Function { get; }
        public List<RestrictedCall> RestrictedCalls { get; } = new List<RestrictedCall>();
        public IValueSolve OnCompleted { get; }

        readonly ScriptFile _script;
        readonly Dictionary<IRecursiveCallHandler, List<DocRange>> _calls = new Dictionary<IRecursiveCallHandler, List<DocRange>>();

        public IEnumerable<IRecursiveCallHandler> Calls => _calls.Select(c => c.Key);

        public CallInfo(IRecursiveCallHandler function, ScriptFile script, IValueSolve onCompleted = null)
        {
            Function = function;
            _script = script;
            OnCompleted = onCompleted ?? new ValueSolveSource(true);
        }

        public CallInfo(ScriptFile script)
        {
            _script = script;
        }

        public void Call(IRecursiveCallHandler callBlock, DocRange range)
        {
            // Add the call to the dictionary if it does not exist.
            if (!_calls.ContainsKey(callBlock))
                _calls.Add(callBlock, new List<DocRange>());
            
            // Add the call range.
            _calls[callBlock].Add(range);
        }

        public void CheckRecursion(DeltinScript deltinScript)
        {
            foreach (var call in _calls)
                if (DoesTreeCall(Function, call.Key))
                    foreach (DocRange range in call.Value)
                        _script.Diagnostics.Error($"Recursion is not allowed here, the {call.Key.TypeName} '{call.Key.GetLabel(deltinScript)}' calls '{Function.GetLabel(deltinScript)}'.", range);
        }

        private bool DoesTreeCall(IRecursiveCallHandler function, IRecursiveCallHandler currentCheck, List<IRecursiveCallHandler> check = null)
        {
            if (check == null) check = new List<IRecursiveCallHandler>();
            if (currentCheck.CallInfo == null) return false;

            if (function.DoesRecursivelyCall(currentCheck))
                return !currentCheck.CanBeRecursivelyCalled();

            if (check.Contains(currentCheck)) return false;
            check.Add(currentCheck);

            foreach (var call in currentCheck.CallInfo._calls)
                if (DoesTreeCall(function, call.Key, check))
                    return true;
            return false;
        }

        public void AddRestrictedCall(RestrictedCall restrictedCall) => RestrictedCalls.Add(restrictedCall);

        public void CheckRestrictedCalls(RuleEvent eventType)
        {
            // Iterate through each restricted call.
            foreach (RestrictedCall call in RestrictedCalls)
                // If the restricted call type's list of supported event types does not contain eventType...
                if (!Deltin.Deltinteger.RestrictedCall.SupportedGroups[call.CallType].Contains(eventType))
                    // ...then add the syntax error.
                    call.AddDiagnostic(_script.Diagnostics);
        }

        public IEnumerable<RestrictedCallType> GetRestrictedCallTypes() => GetRestrictedCallTypes(RestrictedCalls);

        public static IEnumerable<RestrictedCallType> GetRestrictedCallTypes(List<RestrictedCall> restrictedCalls)
        {
            var callTypes = new HashSet<RestrictedCallType>();
            foreach (RestrictedCall call in restrictedCalls)
                if (callTypes.Add(call.CallType))
                    yield return call.CallType;
        }
    }
}