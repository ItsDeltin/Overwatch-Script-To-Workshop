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
            {
                var callChain = DoesTreeCall(Function, call.Key);
                if (callChain is not null)
                {
                    foreach (DocRange range in call.Value)
                    {
                        var path = callChain.GetPathString(deltinScript);
                        _script.Diagnostics.Error($"Recursion is not allowed here, the {call.Key.TypeName} '{call.Key.GetLabel(deltinScript)}' calls '{Function.GetLabel(deltinScript)}'.\n\n{path}", range);
                    }
                }
            }
        }

        private ObjectChain DoesTreeCall(IRecursiveCallHandler function, IRecursiveCallHandler currentCheck)
        {
            var allCheckedItems = new HashSet<IRecursiveCallHandler>();
            ObjectChain InnerDoesTreeCall(ObjectChain callChain)
            {
                var current = callChain.Value;

                // This object has no call information that needs to be considered.
                if (callChain.Value.CallInfo is null)
                    return null;

                // 'current' is a recursive object, such as a 'recursive' method or subroutine.
                // Any subcalls are valid, so we can end here.
                if (current.CanBeRecursivelyCalled())
                    return null;

                // The condition below determines if we have discovered a loop.
                // This is where we begin to error.
                if (function.IsEqualTo(current))
                    return callChain;

                // If this object was already considered, there is no need to look at the subcalls again.
                // Also prevents infinite loop here.
                if (!allCheckedItems.Add(current))
                    return null;

                // Analyze the subcalls.
                foreach (var call in callChain.Value.CallInfo._calls)
                {
                    var innerCall = InnerDoesTreeCall(callChain.CreateChild(call.Key));
                    if (innerCall is not null)
                        return innerCall;
                }
                return null;
            }
            return InnerDoesTreeCall(new ObjectChain(function).CreateChild(currentCheck));
        }

        public void AddRestrictedCall(RestrictedCall restrictedCall) => RestrictedCalls.Add(restrictedCall);

        public void CheckRestrictedCalls(RuleEvent eventType)
        {
            // Iterate through each restricted call.
            foreach (RestrictedCall call in RestrictedCalls)
                // If the restricted call type's list of supported event types does not contain eventType...
                if (RestrictedCall.SupportedGroups.TryGetValue(call.CallType, out var group) && !group.Contains(eventType))
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

#nullable enable
        class ObjectChain
        {
            public ObjectChain? Parent { get; init; }
            public IRecursiveCallHandler Value { get; }

            public ObjectChain(IRecursiveCallHandler value) => Value = value;
            public ObjectChain CreateChild(IRecursiveCallHandler value) => new(value) { Parent = this };

            public string GetPathString(DeltinScript ds)
            {
                string output = $"'{Value.GetLabel(ds)}'";
                var active = Parent;
                while (active is not null)
                {
                    output = $"'{active.Value.GetLabel(ds)}' -> {output}";
                    active = active.Parent;
                }
                return output;
            }
        }
    }
}