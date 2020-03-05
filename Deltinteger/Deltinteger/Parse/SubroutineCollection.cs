using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class SubroutineCollection
    {
        private List<Subroutine> Subroutines { get; } = new List<Subroutine>();
        private int CurrentID = 0;

        public Subroutine NewSubroutine(string name)
        {
            // Get the next available name.
            name = MetaElement.WorkshopNameFromCodeName(name, Subroutines.Select(sr => sr.Name).ToArray());

            Subroutine newRoutine = new Subroutine(CurrentID, name);
            CurrentID++;
            Subroutines.Add(newRoutine);
            
            return newRoutine;
        }

        public void ToWorkshop(WorkshopBuilder builder)
        {
            if (Subroutines.Count == 0) return;

            builder.AppendKeywordLine("subroutines");
            builder.AppendLine("{");
            builder.Indent();

            foreach (Subroutine routine in Subroutines)
                builder.AppendLine(routine.ID.ToString() + ": " + routine.Name);

            builder.Unindent();
            builder.AppendLine("}");
            builder.AppendLine();
        }
    }

    public class SubroutineStack
    {
        private readonly DeltinScript _deltinScript;
        public ReturnHandler ReturnHandler { get; }
        public Subroutine Subroutine { get; }
        public TranslateRule Rule { get; }
        public ActionSet ActionSet { get; }

        private IndexReference GlobalRunning;
        private IndexReference PlayerRunning;
        private IndexReference CurrentCallPlayer;
        private IndexReference CurrentCallGlobal;
        private IndexReference GlobalIndexAssigner;
        private IndexReference PlayerIndexAssigner;

        public SubroutineStack(DeltinScript deltinScript, string subroutineName, string ruleName)
        {
            _deltinScript = deltinScript;

            // Setup the subroutine element.
            Subroutine = deltinScript.SubroutineCollection.NewSubroutine(subroutineName);

            // Create the rule.
            Rule = new TranslateRule(deltinScript, subroutineName, Subroutine);

            // Setup the return handler.
            ReturnHandler = new ReturnHandler(Rule.ActionSet, subroutineName, true);
            ActionSet = Rule.ActionSet.New(ReturnHandler);

            AsyncLock();


        }

        private void AsyncLock()
        {
            V_EventPlayer eventPlayer = new V_EventPlayer();

            ActionSet.AddAction(Element.Part<A_While>(
                Element.Part<V_Or>(
                    Element.Part<V_And>(
                        // The subroutine is executing on a global context,
                        new V_Compare(eventPlayer, Operators.Equal, new V_Null()),
                        // ...and the subroutine is running.
                        GlobalRunning.GetVariable()
                    ),
                    // OR
                    Element.Part<V_And>(
                        // The subroutine is executing on a player context,
                        new V_Compare(eventPlayer, Operators.NotEqual, new V_Null()),
                        // ...and the subroutine is running on the player context.
                        PlayerRunning.GetVariable(eventPlayer)
                    )
                )
            ));

            // While the above while is running, wait.
            ActionSet.AddAction(A_Wait.MinimumWait);
            ActionSet.AddAction(new A_End());

            // When it ends, set the context to true.
            ActionSet.AddAction(PlayerRunning.SetVariable(true, eventPlayer)); // Shouldn't do anything on a global context.
            ActionSet.AddAction(Element.Part<A_If>(new V_Compare(eventPlayer, Operators.Equal, new V_Null())));
            ActionSet.AddAction(GlobalRunning.SetVariable(true));
        }

        public void Apply()
        {
            // Apply returns.
            ReturnHandler.ApplyReturnSkips();

            // Add the subroutine.
            _deltinScript.WorkshopRules.Add(Rule.GetRule());
        }

        public void Call()
        {

        }
    }
}