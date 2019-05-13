using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Deltin.OverwatchParser.Elements
{
    public class Rule
    {
        private static int NumberOfRules = 0; // Required for navigating the ruleset.

        public string Name { get; private set; }
        public RuleEvent RuleEvent { get; private set; }
        public TeamSelector Team { get; private set; }
        public PlayerSelector Player { get; private set; }

        public Condition[] Conditions { get; set; }
        public Element[] Actions { get; set; }

        public Rule(string name, RuleEvent ruleEvent, TeamSelector team, PlayerSelector player) // Creates a rule.
        {
            if (name.Length > Constants.RULE_NAME_MAX_LENGTH)
                throw new ArgumentOutOfRangeException(nameof(name), name, $"Rule names cannot be longer than {Constants.RULE_NAME_MAX_LENGTH} characters.");

            Name = name;
            RuleEvent = ruleEvent;
            Team = team;
            Player = player;
        }

        public Rule(string name) // Creates a "Ongoing - Global" rule.
        {
            Name = name;
            RuleEvent = RuleEvent.Ongoing_Global;
        }

        public void Input()
        {
            // Create rule.
            InputHandler.Input.KeyPress(Keys.Space);
            Thread.Sleep(InputHandler.MediumStep);

            NumberOfRules++;

            // Select rule name.
            InputHandler.Input.RepeatKey(Keys.Down, NumberOfRules);

            InputHandler.Input.KeyPress(Keys.Right);
            Thread.Sleep(InputHandler.SmallStep);

            // Input the name.
            InputHandler.Input.TextInput(Name);
            Thread.Sleep(InputHandler.SmallStep);

            // Leave name input menu.
            InputHandler.Input.KeyPress(Keys.Tab);
            Thread.Sleep(InputHandler.SmallStep);

            // Leaving the input menu with tab resets the controller position.

            // Select the event type.
            InputHandler.Input.RepeatKey(Keys.Down, NumberOfRules + 1);

            InputHandler.Input.SelectEnumMenuOption(RuleEvent);

            // If the rule's event is not global, set the team and player settings.
            if (RuleEvent != RuleEvent.Ongoing_Global)
            {
                // Set the team setting.
                InputHandler.Input.KeyPress(Keys.Down);
                Thread.Sleep(InputHandler.SmallStep);
                InputHandler.Input.SelectEnumMenuOption(Team);

                // Set the player setting.
                InputHandler.Input.KeyPress(Keys.Down);
                Thread.Sleep(InputHandler.SmallStep);
                InputHandler.Input.SelectEnumMenuOption(Player);
            }

            InputHandler.Input.KeyPress(Keys.Down); // Hovering over the "Add Action" button
            Thread.Sleep(InputHandler.SmallStep);

            InputHandler.Input.KeyPress(Keys.Left); // Hovering over the "Add Condition" button
            Thread.Sleep(InputHandler.SmallStep);

            foreach (Condition condition in Conditions)
                condition.Input();
        }
    }
}
