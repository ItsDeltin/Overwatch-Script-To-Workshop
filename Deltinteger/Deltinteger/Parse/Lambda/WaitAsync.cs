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
            DeltinScript.InitialGlobal.ActionSet.AddAction(_waitAsyncQueue.SetVariable(Element.EmptyArray()));

            // Rule creator.
            var rule = new TranslateRule(DeltinScript, "waitAsync", RuleEvent.OngoingGlobal);
            rule.Conditions.Add(new Condition(
                Element.Any(_waitAsyncQueue.Get(), ArrayElementTimeSurpassed(Element.ArrayElement()))
            ));

            // Get the affected item.
            var item = DeltinScript.VarCollection.Assign("waitAsync_item", true, false);
            rule.ActionSet.AddAction(item.SetVariable(Element.FirstOf(Element.Filter(
                Element.Map(_waitAsyncQueue.Get(), Element.ArrayIndex()),
                ArrayElementTimeSurpassed(_waitAsyncQueue.Get()[Element.ArrayElement()])
            ))));

            // Activate item lambda.
            DeltinScript.GetComponent<LambdaGroup>().Call(rule.ActionSet.New(Element.LastOf(_waitAsyncQueue.Get()[item.Get()])), new CallHandler());

            // Remove from queue.
            rule.ActionSet.AddAction(_waitAsyncQueue.ModifyVariable(Operation.RemoveFromArrayByIndex, item.Get()));

            // Loop if another item needs to execute on the same tick.
            rule.ActionSet.AddAction(Element.LoopIfConditionIsTrue());

            // Get the rule.
            DeltinScript.WorkshopRules.Add(rule.GetRule());
        }

        private static Element ArrayElementTimeSurpassed(Element element) => Element.TimeElapsed() >= element[0];

        public void AddToQueue(ActionSet actionSet, Element seconds, Element function)
        {
            actionSet.AddAction(_waitAsyncQueue.ModifyVariable(Operation.AppendToArray, Element.CreateArray(Element.CreateArray(Element.TimeElapsed() + seconds, function))));
        }
    }

    class WaitAsyncFunction : IMethod
    {
        private static string _documentation = new MarkupBuilder().Add("Waits without blocking the current rule, executing the provided action when the wait completes.").NewLine()
            .Add("Using ").Code("Wait").Add(" inside a subroutine that interacts with variables may result in weird and unexpected behaviours when executed from multiples places at once due to race conditions. ")
                .Code("WaitAsync").Add(" will not have this issue.")
            .NewLine().Add("You can not run the rule below multiple times simultaneously because the ").Code("Wait").Add(" will block the rule.").NewLine()
                .StartCodeLine().Add("rule: \"My Rule\"").NewLine().Add("if(IsButtonHeld(HostPlayer(), Button.Interact))").NewLine().Add("{").NewLine()
                .Add("    x = RandomInteger(0, 10);").NewLine().NewLine().Add("    SmallMessage(AllPlayers(), x);").NewLine().Add("    Wait(3)").NewLine().Add("    SmallMessage(AllPlayers(), x);")
                .NewLine().Add("}").EndCodeLine().NewLine()
            .Add("However, using ").Code("WaitAsync").Add(", the rule can be executed multiple times simultaneously.").NewLine()
                .StartCodeLine().Add("rule: \"My Rule\"").NewLine().Add("if(IsButtonHeld(HostPlayer(), Button.Interact))").NewLine().Add("{").NewLine()
                .Add("    x = RandomInteger(0, 10);").NewLine().NewLine().Add("    SmallMessage(AllPlayers(), x);").NewLine().Add("    WaitAsync(3, () => {").NewLine().Add("        SmallMessage(AllPlayers(), x);")
                .NewLine().Add("    });").NewLine().Add("}").EndCodeLine()
            .ToString();

        public string Name => "WaitAsync";
        public MarkupBuilder Documentation => _documentation;
        public CodeType CodeType => null;
        public bool Static => true;
        public bool DoesReturnValue => false;
        public bool WholeContext => true;
        public LanguageServer.Location DefinedAt => null;
        public AccessLevel AccessLevel => AccessLevel.Public;
        public MethodAttributes Attributes { get; } = new MethodAttributes();
        public CodeParameter[] Parameters { get; }

        public WaitAsyncFunction(ITypeSupplier types)
        {
            Parameters = new CodeParameter[] {
                new CodeParameter("duration", "The duration to wait in seconds before the action gets execute.", types.Number()),
                new CodeParameter("action", "The action that is executed when the wait completes.", new PortableLambdaType(LambdaKind.Portable))
            };
        }

        public CompletionItem GetCompletion() => IMethod.GetFunctionCompletion(this);
        public string GetLabel(bool markdown) => LanguageServer.HoverHandler.GetLabel("void", Name, Parameters, markdown, null);

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            actionSet.DeltinScript.GetComponent<WaitAsyncComponent>().AddToQueue(actionSet, methodCall.Get(0), methodCall.Get(1));
            return null;
        }
    }
}