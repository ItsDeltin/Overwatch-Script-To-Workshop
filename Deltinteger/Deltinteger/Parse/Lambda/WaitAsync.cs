using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.FunctionBuilder;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Parse.Lambda
{
    class WaitAsyncComponent : IComponent
    {
        public DeltinScript DeltinScript { get; set; }
        private IndexReference _waitAsyncQueue;

        // IComponent
        public void Init()
        {
            _waitAsyncQueue = DeltinScript.VarCollection.Assign("waitAsync_queue", true, false);
            DeltinScript.InitialGlobal.ActionSet.AddAction(_waitAsyncQueue.SetVariable(new V_EmptyArray()));

            // Rule creator.
            var rule = new TranslateRule(DeltinScript, "waitAsync", RuleEvent.OngoingGlobal);
            rule.Conditions.Add(new Condition(
                Element.Part<V_IsTrueForAny>(_waitAsyncQueue.Get(), ArrayElementTimeSurpassed(new V_ArrayElement()))
            ));

            // Get the affected item.
            var item = DeltinScript.VarCollection.Assign("waitAsync_item", true, false);
            rule.ActionSet.AddAction(item.SetVariable(Element.Part<V_FirstOf>(Element.Part<V_FilteredArray>(
                Element.Part<V_MappedArray>(_waitAsyncQueue.Get(), new V_CurrentArrayIndex()),
                ArrayElementTimeSurpassed(_waitAsyncQueue.Get()[new V_ArrayElement()])
            ))));

            // Activate item lambda.
            DeltinScript.GetComponent<LambdaGroup>().Call(rule.ActionSet.New(Element.Part<V_LastOf>(_waitAsyncQueue.Get()[item.Get()])), new CallHandler());

            // Remove from queue.
            rule.ActionSet.AddAction(_waitAsyncQueue.ModifyVariable(Operation.RemoveFromArrayByIndex, item.Get()));

            // Loop if another item needs to execute on the same tick.
            rule.ActionSet.AddAction(Element.Part<A_LoopIfConditionIsTrue>());

            // Get the rule.
            DeltinScript.WorkshopRules.Add(rule.GetRule());
        }

        private static Element ArrayElementTimeSurpassed(Element element) => Element.Part<V_TotalTimeElapsed>() >= element[0];

        public void AddToQueue(ActionSet actionSet, Element seconds, Element function)
        {
            actionSet.AddAction(_waitAsyncQueue.ModifyVariable(Operation.AppendToArray, Element.CreateArray(Element.CreateArray(Element.Part<V_TotalTimeElapsed>() + seconds, function))));
        }
    }

    class WaitAsyncFunction : IMethod
    {
        public string Name => "WaitAsync";
        public string Documentation => "Asynchronously waits.";
        public CodeType CodeType => null;
        public bool Static => true;
        public bool DoesReturnValue => false;
        public bool WholeContext => true;
        public LanguageServer.Location DefinedAt => null;
        public AccessLevel AccessLevel => AccessLevel.Public;
        public MethodAttributes Attributes { get; } = new MethodAttributes();
        public CodeParameter[] Parameters { get; } = new CodeParameter[] {
            new CodeParameter("duration"),
            new CodeParameter("action", new PortableLambdaType(LambdaKind.Portable))
        };
        public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);
        public string GetLabel(bool markdown) => LanguageServer.HoverHandler.GetLabel("void", Name, Parameters, markdown, null);

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            actionSet.DeltinScript.GetComponent<WaitAsyncComponent>().AddToQueue(actionSet, methodCall.Get(0), methodCall.Get(1));
            return null;
        }
    }
}