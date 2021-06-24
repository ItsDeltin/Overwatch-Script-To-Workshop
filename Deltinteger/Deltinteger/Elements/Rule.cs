using Deltin.Deltinteger.I18n;

namespace Deltin.Deltinteger.Elements
{
    public class Rule
    {
        public string Name { get; }
        public RuleEvent RuleEvent { get; }
        public Team Team { get; }
        public PlayerSelector Player { get; }
        public Subroutine Subroutine { get; set; }
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

        public Rule(string name, Subroutine subroutine)
        {
            Name = name;
            RuleEvent = RuleEvent.Subroutine;
            RuleType = RuleType.Subroutine;
            Subroutine = subroutine;
        }

        public override string ToString() => Name;

        public void ToWorkshop(WorkshopBuilder builder)
        {
            if (Disabled)
            {
                builder.AppendKeyword("disabled")
                    .Append(" ");
            }
            builder.AppendKeyword("rule")
                .AppendLine("(\"" + Name + "\")")
                .AppendLine("{")
                .AppendLine()
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
                    Subroutine.ToWorkshop(builder, ToWorkshopContext.Other); // Attribute name
                    builder.Append(";").AppendLine();
                    break;
            }
            builder.Outdent()
                .AppendLine("}");

            if (Conditions?.Length > 0)
            {
                builder.AppendLine()
                    .AppendKeywordLine("conditions")
                    .AppendLine("{")
                    .Indent();

                foreach (var condition in Conditions)
                    condition.ToWorkshop(builder);
                
                builder.Outdent().AppendLine("}");
            }

            // Add actions.
            if (Actions?.Length > 0)
            {
                builder.AppendLine()
                    .AppendLine("// Action count: " + Actions.Length) // Action count comment.
                    .AppendKeywordLine("actions")
                    .AppendLine("{")
                    .Indent();

                foreach (var action in Actions)
                    action.ToWorkshop(builder, ToWorkshopContext.Action);
                
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
