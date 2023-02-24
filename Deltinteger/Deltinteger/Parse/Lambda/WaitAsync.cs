using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Lambda
{
    // TODO: convert to IWorkshopComponent
    class WaitAsyncComponent : IComponent
    {
        public DeltinScript DeltinScript { get; set; }
        private IndexReference _waitAsyncQueue;

        // IComponent
        public void Init(DeltinScript deltinScript)
        {
            DeltinScript = deltinScript;

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
            DeltinScript.WorkshopConverter.LambdaBuilder.Call(rule.ActionSet.New(Element.LastOf(_waitAsyncQueue.Get()[item.Get()])), new Functions.Builder.CallInfo(), null);

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

        public static FuncMethod Method(ITypeSupplier types) => new FuncMethodBuilder()
        {
            Name = "WaitAsync",
            Documentation = new MarkupBuilder().Add("Waits without blocking the current rule, executing the provided action when the wait completes.").NewLine()
                .Add("Using ").Code("Wait").Add(" inside a subroutine that interacts with variables may result in weird and unexpected behaviours when executed from multiples places at once due to race conditions. ")
                    .Code("WaitAsync").Add(" will not have this issue.")
                .NewLine().Add("You can not run the rule below multiple times simultaneously because the ").Code("Wait").Add(" will block the rule.").NewLine()
                    .StartCodeLine().Add("rule: \"My Rule\"").NewLine().Add("if(IsButtonHeld(HostPlayer(), Button.Interact))").NewLine().Add("{").NewLine()
                    .Add("    x = RandomInteger(0, 10);").NewLine().NewLine().Add("    SmallMessage(AllPlayers(), x);").NewLine().Add("    Wait(3)").NewLine().Add("    SmallMessage(AllPlayers(), x);")
                    .NewLine().Add("}").EndCodeLine().NewLine()
                .Add("However, using ").Code("WaitAsync").Add(", the rule can be executed multiple times simultaneously.").NewLine()
                    .StartCodeLine().Add("rule: \"My Rule\"").NewLine().Add("if(IsButtonHeld(HostPlayer(), Button.Interact))").NewLine().Add("{").NewLine()
                    .Add("    x = RandomInteger(0, 10);").NewLine().NewLine().Add("    SmallMessage(AllPlayers(), x);").NewLine().Add("    WaitAsync(3, () => {").NewLine().Add("        SmallMessage(AllPlayers(), x);")
                    .NewLine().Add("    });").NewLine().Add("}").EndCodeLine(),
            Parameters = new[] {
                new CodeParameter("duration", "The duration to wait in seconds before the action gets execute.", types.Number()),
                new CodeParameter("action", "The action that is executed when the wait completes.", new PortableLambdaType(new(LambdaKind.Portable)))
            },
            Action = (actionSet, methodCall) =>
            {
                actionSet.DeltinScript.GetComponent<WaitAsyncComponent>().AddToQueue(actionSet, methodCall.Get(0), methodCall.Get(1));
                return null;
            }
        };
    }
}