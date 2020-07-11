using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
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

        public void CheckRecursion()
        {
            foreach (var call in Calls)
                if (DoesTreeCall(Function, call.Key))
                    foreach (DocRange range in call.Value)
                        Script.Diagnostics.Error($"Recursion is not allowed here, the {call.Key.TypeName} '{call.Key.GetLabel()}' calls '{Function.GetLabel()}'.", range);
        }

        private bool DoesTreeCall(IRecursiveCallHandler function, IRecursiveCallHandler currentCheck, List<IRecursiveCallHandler> check = null)
        {
            if (check == null) check = new List<IRecursiveCallHandler>();
            if (currentCheck.CallInfo == null) return false;

            // if (function is DefinedMethod dm && currentCheck is IMethod asMethod && (dm == asMethod || asMethod.Attributes.AllOverrideOptions().Contains(dm)))
            //     return !asMethod.Attributes.Recursive;
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
                    Script.Diagnostics.Error(call.CallStrategy.Message(), call.CallRange.range);
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
        string GetLabel();
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
        public string GetLabel() => _applyBlock.GetLabel(false);
    }

    public interface IRestrictedCallHandler
    {
        void RestrictedCall(RestrictedCall restrictedCall);
    }
}