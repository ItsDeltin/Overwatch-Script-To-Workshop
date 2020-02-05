using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class CallInfo
    {
        public IApplyBlock Function { get; }
        private ScriptFile Script { get; }
        private Dictionary<IApplyBlock, List<DocRange>> Calls { get; } = new Dictionary<IApplyBlock, List<DocRange>>();

        public CallInfo(IApplyBlock function, ScriptFile script)
        {
            Function = function;
            Script = script;
        }

        public void Call(IApplyBlock callBlock, DocRange range)
        {
            if (!Calls.ContainsKey(callBlock)) Calls.Add(callBlock, new List<DocRange>());
            Calls[callBlock].Add(range);
        }

        public void CheckRecursion()
        {
            foreach (var call in Calls)
                if (DoesTreeCall(Function, call.Key))
                    foreach (DocRange range in call.Value)
                        Script.Diagnostics.Error($"Recursion is not allowed here, the function '{call.Key.GetLabel(false)}' calls '{Function.GetLabel(false)}'.", range);
        }

        private bool DoesTreeCall(IApplyBlock function, IApplyBlock currentCheck, List<IApplyBlock> check = null)
        {
            if (check == null) check = new List<IApplyBlock>();
            if (currentCheck.CallInfo == null) return false;

            if (function == currentCheck)
            {
                if (function is DefinedMethod && ((DefinedMethod)function).IsRecursive)
                    return false;
                else
                    return true;
            }

            if (check.Contains(currentCheck)) return false;
            check.Add(currentCheck);

            foreach (var call in currentCheck.CallInfo.Calls)
                if (DoesTreeCall(function, call.Key, check))
                    return true;
            return false;
        }
    }
}