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

        public string ToWorkshop(OutputLanguage language, bool optimize)
        {
            var builder = new TabStringBuilder(true);

            builder.Indent = 0;
            if (Disabled)
                builder.Append(I18n.I18n.Translate(language, "disabled") + " ");
            builder.AppendLine($"{I18n.I18n.Translate(language, "rule")}(\"{Name}\")");
            builder.AppendLine("{");
            builder.AppendLine();

            builder.Indent = 1;
            builder.AppendLine(I18n.I18n.Translate(language, "event"));
            builder.AppendLine("{");
            builder.Indent = 2;
            builder.AppendLine(EnumData.GetEnumValue(RuleEvent)
                .ToWorkshop(language) + ";");
            
            // Add attributes.
            switch (RuleType)
            {
                case RuleType.PlayerBased:
                    // Player based attributes
                    builder.AppendLine(EnumData.GetEnumValue(Team).ToWorkshop(language) + ";"); // Team attribute
                    builder.AppendLine(EnumData.GetEnumValue(Player).ToWorkshop(language) + ";"); // Player attribute
                    break;
                
                case RuleType.Subroutine:
                    builder.AppendLine(Subroutine.ToWorkshop(language) + ";"); // Attribute name
                    break;
            }
            builder.Indent = 1;
            builder.AppendLine("}");

            if (Conditions?.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine(I18n.I18n.Translate(language, "conditions"));
                builder.AppendLine("{");
                builder.Indent = 2;
                foreach (var condition in Conditions)
                    builder.AppendLine(condition.ToWorkshop(language, optimize) + ";");
                builder.Indent = 1;
                builder.AppendLine("}");
            }

            // Add actions.
            if (Actions?.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine("// Action count: " + Actions.Length); // Action count comment.
                builder.AppendLine(I18n.I18n.Translate(language, "actions"));
                builder.AppendLine("{");
                builder.Indent = 2;

                foreach (var action in Actions)
                    if (optimize)
                        builder.AppendLine(action.Optimize().ToWorkshop(language));
                    else
                        builder.AppendLine(action.ToWorkshop(language));
                
                builder.Indent = 1;
                builder.AppendLine("}");
            }
            builder.Indent = 0;
            builder.AppendLine("}");

            return builder.ToString();
        }
    }

    public enum RuleType
    {
        Global,
        PlayerBased,
        Subroutine
    }
}
