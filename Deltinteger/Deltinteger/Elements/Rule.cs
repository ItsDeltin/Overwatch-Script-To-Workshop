using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Deltin.Deltinteger.Elements
{
    public class Rule: IWorkshopTree
    {
        public string Name { get; private set; }
        public RuleEvent RuleEvent { get; private set; }
        public Team Team { get; private set; }
        public PlayerSelector Player { get; private set; }
        public bool IsGlobal { get; private set; }

        public Condition[] Conditions { get; set; }
        public Element[] Actions { get; set; }

        public Rule(string name, RuleEvent ruleEvent = RuleEvent.OngoingGlobal, Team team = Team.All, PlayerSelector player = PlayerSelector.All) // Creates a rule.
        {
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

        public void DebugPrint(Log log, int depth = 0)
        {
            log.Write(LogLevel.Verbose, new ColorMod("Conditions:", ConsoleColor.DarkYellow));
            if (Conditions != null)
                foreach (var condition in Conditions)
                    condition.DebugPrint(log, 1);
            
            log.Write(LogLevel.Verbose, new ColorMod("Actions:", ConsoleColor.DarkCyan));
            if (Actions != null)
                foreach (var action in Actions)
                    action.DebugPrint(log, 1);
        }

        public string ToWorkshop()
        {            
            var builder = new TabStringBuilder(true);

            builder.Indent = 0;                                                       //
            builder.AppendLine($"rule(\"{Name}\")");                                  // rule("this is the name of the rule!")
            builder.AppendLine("{");                                                  // {
            builder.AppendLine();                                                     //
                                                                                      //
            builder.Indent = 1;                                                       // (indent)
            builder.AppendLine("event");                                              //     event
            builder.AppendLine("{");                                                  //     {
            builder.Indent = 2;                                                       //     (indent)
            builder.AppendLine(EnumData.GetEnumValue(RuleEvent).ToWorkshop() + ";");  //         Ongoing - Each Player
            if (!IsGlobal)                                                            //       --(only if the event is a player event)
            {                                                                         //       |  
                builder.AppendLine(EnumData.GetEnumValue(Team).ToWorkshop() + ";");   //       | Team 1
                builder.AppendLine(EnumData.GetEnumValue(Player).ToWorkshop() + ";"); //       | Bastion
            }                                                                         //
            builder.Indent = 1;                                                       //     (outdent)
            builder.AppendLine("}");                                                  //     }
                                                                                      //
            if (Conditions?.Length > 0)                                               // (only if there are 1 or more conditions)
            {                                                                         // |
                builder.AppendLine();                                                 // |
                builder.AppendLine("conditions");                                     // |   conditions
                builder.AppendLine("{");                                              // |   {
                builder.Indent = 2;                                                   // |   (indent)
                foreach (var condition in Conditions)                                 // |       
                    builder.AppendLine(condition.ToWorkshop() + ";");                 // |       Number Of Players >= 3;
                builder.Indent = 1;                                                   // |   (outdent)
                builder.AppendLine("}");                                              // |   }
            }                                                                         //
                                                                                      //
            if (Actions?.Length > 0)                                                  // (only if there are 1 or more actions)
            {                                                                         // |
                builder.AppendLine();                                                 // |
                builder.AppendLine("// Action count: " + Actions.Length);             // |   // Action count: #
                builder.AppendLine("actions");                                        // |   actions
                builder.AppendLine("{");                                              // |   {
                builder.Indent = 2;                                                   // |   (indent)
                foreach (var action in Actions)                                       // |       
                    builder.AppendLine(action.ToWorkshop());                          // |       Set Global Variable(A, true);
                builder.Indent = 1;                                                   // |   (outdent)
                builder.AppendLine("}");                                              // |   }
            }                                                                         //
            builder.Indent = 0;                                                       // (outdent)
            builder.AppendLine("}");                                                  // }

            return builder.ToString();
        }
    }
}
