using System;
using System.Linq;

namespace Deltin.Deltinteger.Elements
{
    public class Rule
    {
        public string Name { get; }
        public RuleEvent RuleEvent { get; }
        public Team Team { get; }
        public PlayerSelector Player { get; }
        public string Subroutine { get; set; }
        public RuleType RuleType { get; }

        public Condition[] Conditions { get; set; }
        public Element[] Actions { get; set; }

        public bool Disabled { get; set; }

        public double Priority { get; set; }

        public Rule(string name, RuleEvent ruleEvent = RuleEvent.OngoingGlobal, Team team = Team.All, PlayerSelector player = PlayerSelector.All) // Creates a rule.
        {
            if (ruleEvent == RuleEvent.OngoingGlobal && (team != Team.All || player != PlayerSelector.All))
                ruleEvent = RuleEvent.OngoingPlayer;

            Name = name;
            RuleEvent = ruleEvent;
            Team = team;
            Player = player;

            if (RuleEvent == RuleEvent.OngoingGlobal) RuleType = RuleType.Global;
            else if (RuleEvent == RuleEvent.Subroutine) RuleType = RuleType.Subroutine;
            else RuleType = RuleType.PlayerBased;
        }

        public Rule(string name, string subroutine)
        {
            Name = name;
            RuleEvent = RuleEvent.Subroutine;
            RuleType = RuleType.Subroutine;
            Subroutine = subroutine;
        }

        public override string ToString() => Name;

        public void ToWorkshop(WorkshopBuilder builder)
        {
            
            // Element count comment.
            if (builder.IncludeComments)
                builder.AppendLine("// Rule Element Count: " + ElementCount());

            if (Disabled)
            {
                builder.AppendKeyword("disabled")
                    .Append(" ");
            }
            builder.AppendKeyword("rule")
                .AppendLine("(\"" + Name + "\")")
                .AppendLine("{")
                .Indent()
                .AppendKeywordLine("event")
                .AppendLine("{")
                .Indent();

            ElementRoot.Instance.GetEnumValue("Event", RuleEvent.ToString()).ToWorkshop(builder, ToWorkshopContext.Other);
            builder.Append(";").AppendLine();

            // Add attributes.
            switch (RuleType)
            {
                case RuleType.PlayerBased:
                    // Player based attributes
                    ElementEnumMember.Team(Team).ToWorkshop(builder, ToWorkshopContext.Other); // Team attribute
                    builder.Append(";").AppendLine();
                    ElementRoot.Instance.GetEnumValue("Player", Player.ToString()).ToWorkshop(builder, ToWorkshopContext.Other); // Player attribute
                    builder.Append(";").AppendLine();
                    break;

                case RuleType.Subroutine:
                    builder.Append(Subroutine).Append(";").AppendLine();
                    break;
            }
            builder.Outdent()
                .AppendLine("}");

            if (Conditions?.Length > 0) {
                builder.AppendLine();
                    
                if (builder.IncludeComments)
                    builder.AppendLine("// Element Count: " + Conditions.Sum(x => x.ElementCount()) + ", Condition Count: " + Conditions.Length);
                
                builder.AppendKeywordLine("conditions")
                    .AppendLine("{")
                    .Indent();

                foreach (var condition in Conditions)
                    condition.ToWorkshop(builder);

                builder.Outdent().AppendLine("}");
            }

            // Add actions.
            if (Actions?.Length > 0)
            {
                builder.AppendLine();

                // Action and element count comment.
                if (builder.IncludeComments)
                {
                    int largestCount = Actions.Max(x => x.ElementCount());
                    Element largestAction = Array.FindIndex(Actions, x => x.ElementCount() == largestCount);
                    int totalElementCount = Actions.Sum(x => x.ElementCount());

                    builder.AppendLine($"// Element Count: {totalElementCount}, Action Count: {Actions.Length}");
                    if (Actions.Length > 1) {
                        builder.AppendLine($"// Largest Action Index: {largestAction} using {largestCount} Elements");
                    }
                       
                }
                
                builder.AppendKeywordLine("actions").AppendLine("{").Indent();
                int resetIndentInCaseOfUnbalance = builder.GetCurrentIndent();

                foreach (var action in Actions)
                    action.ToWorkshop(builder, ToWorkshopContext.Action);

                builder.SetCurrentIndent(resetIndentInCaseOfUnbalance);
                builder.Outdent().AppendLine("}");
            }
            builder.Outdent().AppendLine("}");
        }

        public int ElementCount()
        {
            int count = 1;

            if (Conditions != null)
                foreach (Condition condition in Conditions)
                    count += condition.ElementCount();

            if (Actions != null)
                foreach (Element action in Actions)
                    count += action.ElementCount();

            return count;
        }

        public Rule Optimized()
        {
            // Get new rule.
            Rule optimized = RuleType == RuleType.Subroutine ? new Rule(Name, Subroutine) : new Rule(Name, RuleEvent, Team, Player);

            // Copy other settings.
            optimized.Disabled = Disabled;
            optimized.Priority = Priority;

            // Optimize conditions.
            if (Conditions != null)
            {
                optimized.Conditions = new Condition[Conditions.Length];
                for (int i = 0; i < optimized.Conditions.Length; i++)
                    optimized.Conditions[i] = Conditions[i].Optimized();
            }

            // Optimize actions.
            if (Actions != null)
            {
                optimized.Actions = new Element[Actions.Length];
                for (int i = 0; i < optimized.Actions.Length; i++)
                    optimized.Actions[i] = Actions[i].Optimized();
            }

            return optimized;
        }
    }

    public enum RuleType
    {
        Global,
        PlayerBased,
        Subroutine
    }
}
