using Deltin.Deltinteger.I18n;

namespace Deltin.Deltinteger.Elements
{
    public class Rule
    {
        public string Name { get; }
        public RuleEvent RuleEvent { get; }
        public Team Team { get; }
        public PlayerSelector Player { get; }
        public Subroutine Subroutine { get; }
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

        public override string ToString()
        {
            return Name;
        }

        public void ToWorkshop(WorkshopBuilder builder, bool optimize)
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
                .Indent()
                .AppendLine(ElementRoot.Instance.GetEnumValue("Event", RuleEvent.ToString()).ToWorkshop(builder.OutputLanguage, ToWorkshopContext.Other) + ";");
            
            // Add attributes.
            switch (RuleType)
            {
                case RuleType.PlayerBased:
                    // Player based attributes
                    builder.AppendLine(ElementEnumMember.Team(Team).ToWorkshop(builder.OutputLanguage, ToWorkshopContext.Other) + ";"); // Team attribute
                    builder.AppendLine(ElementRoot.Instance.GetEnumValue("Player", Player.ToString()).ToWorkshop(builder.OutputLanguage, ToWorkshopContext.Other) + ";"); // Player attribute
                    break;
                
                case RuleType.Subroutine:
                    builder.AppendLine(Subroutine.ToWorkshop(builder.OutputLanguage, ToWorkshopContext.Other) + ";"); // Attribute name
                    break;
            }
            builder.Unindent()
                .AppendLine("}");

            if (Conditions?.Length > 0)
            {
                builder.AppendLine()
                    .AppendKeywordLine("conditions")
                    .AppendLine("{")
                    .Indent();

                foreach (var condition in Conditions)
                    builder.AppendLine(condition.ToWorkshop(builder.OutputLanguage, optimize) + ";");
                
                builder.Unindent()
                    .AppendLine("}");
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
                    if (optimize)
                        builder.AppendLine(action.Optimize().ToWorkshop(builder.OutputLanguage, ToWorkshopContext.Action));
                    else
                        builder.AppendLine(action.ToWorkshop(builder.OutputLanguage, ToWorkshopContext.Action));
                
                builder.Unindent()
                    .AppendLine("}");
            }
            builder.Unindent()
                .AppendLine("}");
        }
    
        public int ElementCount(bool optimized)
        {
            int count = 1;

            if (Conditions != null)
                foreach (Condition condition in Conditions)
                    count += condition.ElementCount(optimized);

            if (Actions != null)
                foreach (Element action in Actions)
                {
                    if (optimized)
                        count += action.Optimize().ElementCount();
                    else
                        count += action.ElementCount();
                }
            
            return count;
        }
    }

    public enum RuleType
    {
        Global,
        PlayerBased,
        Subroutine
    }
}
