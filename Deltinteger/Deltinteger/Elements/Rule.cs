using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Deltin.Deltinteger.Elements
{
    public class Rule
    {
        public string Name { get; }
        public RuleEvent RuleEvent { get; }
        public Team Team { get; }
        public PlayerSelector Player { get; }
        public bool IsGlobal { get; }

        public Condition[] Conditions { get; set; }
        public Element[] Actions { get; set; }

        public bool Disabled { get; set; }

        public Rule(string name, RuleEvent ruleEvent = RuleEvent.OngoingGlobal, Team team = Team.All, PlayerSelector player = PlayerSelector.All) // Creates a rule.
        {
            if (ruleEvent == RuleEvent.OngoingGlobal && (team != Team.All || player != PlayerSelector.All))
                ruleEvent = RuleEvent.OngoingPlayer;

            Name = name;
            RuleEvent = ruleEvent;
            Team = team;
            Player = player;
            IsGlobal = ruleEvent == RuleEvent.OngoingGlobal;
        }

        public override string ToString()
        {
            return Name;
        }

        public string ToWorkshop(OutputLanguage language)
        {            
            var builder = new TabStringBuilder(true);

            builder.Indent = 0;
            if (Disabled)
                builder.Append(I18n.I18n.Translate(language, "disabled") + " ");
            builder.AppendLine($"{I18n.I18n.Translate(language, "rule")}(\"{Name}\")"); // rule("this is the name of the rule!")
            builder.AppendLine("{");                                                    // {
            builder.AppendLine();                                                       //
                                                                                        //
            builder.Indent = 1;                                                         // (indent)
            builder.AppendLine(I18n.I18n.Translate(language, "event"));                 //     event
            builder.AppendLine("{");                                                    //     {
            builder.Indent = 2;                                                         //     (indent)
            builder.AppendLine(EnumData.GetEnumValue(RuleEvent)                         //
                .ToWorkshop(language) + ";");                                           //         Ongoing - Each Player
            if (!IsGlobal)                                                              //       --(only if the event is a player event)
            {                                                                           //       |  
                builder.AppendLine(EnumData.GetEnumValue(Team)                          //       |
                    .ToWorkshop(language) + ";");                                       //       | Team 1
                builder.AppendLine(EnumData.GetEnumValue(Player)                        //       |
                    .ToWorkshop(language) + ";");                                       //       | Bastion
            }                                                                           //
            builder.Indent = 1;                                                         //     (outdent)
            builder.AppendLine("}");                                                    //     }
                                                                                        //
            if (Conditions?.Length > 0)                                                 // (only if there are 1 or more conditions)
            {                                                                           // |
                builder.AppendLine();                                                   // |
                builder.AppendLine(I18n.I18n.Translate(language, "conditions"));        // |   conditions
                builder.AppendLine("{");                                                // |   {
                builder.Indent = 2;                                                     // |   (indent)
                foreach (var condition in Conditions)                                   // |       
                    builder.AppendLine(condition.ToWorkshop(language) + ";");           // |       Number Of Players >= 3;
                builder.Indent = 1;                                                     // |   (outdent)
                builder.AppendLine("}");                                                // |   }
            }                                                                           //
                                                                                        //
            if (Actions?.Length > 0)                                                    // (only if there are 1 or more actions)
            {                                                                           // |
                builder.AppendLine();                                                   // |
                builder.AppendLine("// Action count: " + Actions.Length);               // |   // Action count: #
                builder.AppendLine(I18n.I18n.Translate(language, "actions"));           // |   actions
                builder.AppendLine("{");                                                // |   {
                builder.Indent = 2;                                                     // |   (indent)
                foreach (var action in Actions)                                         // |       
                    builder.AppendLine(action.Optimize().ToWorkshop(language));         // |       Set Global Variable(A, true);
                builder.Indent = 1;                                                     // |   (outdent)
                builder.AppendLine("}");                                                // |   }
            }                                                                           //
            builder.Indent = 0;                                                         // (outdent)
            builder.AppendLine("}");                                                    // }

            return builder.ToString();
        }
    }
}
