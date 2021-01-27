using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using RuleEvent = Deltin.Deltinteger.Elements.RuleEvent;

namespace Deltin.Deltinteger.Parse
{
    public class CallInfo : IRestrictedCallHandler
    {
        public IRecursiveCallHandler Function { get; }
        private ScriptFile Script { get; }
        private Dictionary<IRecursiveCallHandler, List<DocRange>> Calls { get; } = new Dictionary<IRecursiveCallHandler, List<DocRange>>();
        public List<RestrictedCall> RestrictedCalls { get; } = new List<RestrictedCall>();

        public CallInfo(IRecursiveCallHandler function, ScriptFile script)
        {
            Function = function;
            Script = script;
        }

        public CallInfo(ScriptFile script)
        {
            Script = script;
        }

        public void Call(IRecursiveCallHandler callBlock, DocRange range)
        {
            if (!Calls.ContainsKey(callBlock)) Calls.Add(callBlock, new List<DocRange>());
            Calls[callBlock].Add(range);
        }

        public void CheckRecursion(DeltinScript deltinScript)
        {
            foreach (var call in Calls)
                if (DoesTreeCall(Function, call.Key))
                    foreach (DocRange range in call.Value)
                        Script.Diagnostics.Error($"Recursion is not allowed here, the {call.Key.TypeName} '{call.Key.GetLabel(deltinScript)}' calls '{Function.GetLabel(deltinScript)}'.", range);
        }

        private bool DoesTreeCall(IRecursiveCallHandler function, IRecursiveCallHandler currentCheck, List<IRecursiveCallHandler> check = null)
        {
            if (check == null) check = new List<IRecursiveCallHandler>();
            if (currentCheck.CallInfo == null) return false;

            if (function.DoesRecursivelyCall(currentCheck))
                return !currentCheck.CanBeRecursivelyCalled();

            if (check.Contains(currentCheck)) return false;
            check.Add(currentCheck);

            foreach (var call in currentCheck.CallInfo.Calls)
                if (DoesTreeCall(function, call.Key, check))
                    return true;
            return false;
        }

        public void RestrictedCall(RestrictedCall restrictedCall) => RestrictedCalls.Add(restrictedCall);

        public void CheckRestrictedCalls(RuleEvent eventType)
        {
            // Iterate through each restricted call.
            foreach (RestrictedCall call in RestrictedCalls)
                // If the restricted call type's list of supported event types does not contain eventType...
                if (!Deltin.Deltinteger.RestrictedCall.SupportedGroups[call.CallType].Contains(eventType))
                    // ...then add the syntax error.
                    call.AddDiagnostic(Script.Diagnostics);
        }

        public RestrictedCallType[] GetRestrictedCallTypes() => GetRestrictedCallTypes(RestrictedCalls);

        public static RestrictedCallType[] GetRestrictedCallTypes(List<RestrictedCall> restrictedCalls)
        {
            List<RestrictedCallType> callTypes = new List<RestrictedCallType>();
            foreach (RestrictedCall call in restrictedCalls) if (!callTypes.Contains(call.CallType)) callTypes.Add(call.CallType);
            return callTypes.ToArray();
        }
    }

    public interface IRecursiveCallHandler
    {
        CallInfo CallInfo { get; }
        string TypeName { get; }
        bool DoesRecursivelyCall(IRecursiveCallHandler calling);
        bool CanBeRecursivelyCalled();
        string GetLabel(DeltinScript deltinScript);
    }

    public class RecursiveCallHandler : IRecursiveCallHandler
    {
        private readonly IApplyBlock _applyBlock;
        private readonly string _typeName;

        public RecursiveCallHandler(IApplyBlock applyBlock, string typeName = "function")
        {
            _applyBlock = applyBlock;
            _typeName = typeName;
        }

        public CallInfo CallInfo => _applyBlock.CallInfo;
        public string TypeName => _typeName;
        public bool DoesRecursivelyCall(IRecursiveCallHandler calling) => this == calling;
        public bool CanBeRecursivelyCalled() => _applyBlock is IMethod function && function.Attributes.Recursive;
        public string GetLabel(DeltinScript deltinScript) => _applyBlock.GetLabel(deltinScript, LabelInfo.RecursionError).ToString(false);
    }

    public interface IRestrictedCallHandler
    {
        void RestrictedCall(RestrictedCall restrictedCall);
    }

    public class RestrictedCallList : List<RestrictedCall>, IRestrictedCallHandler
    {
        public void RestrictedCall(RestrictedCall restrictedCall) => Add(restrictedCall);
    }
}