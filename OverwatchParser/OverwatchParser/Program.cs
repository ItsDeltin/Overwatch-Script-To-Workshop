using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using OverwatchParser;
using OverwatchParser.Elements;
using OverwatchParser.Parse;

namespace OverwatchParser
{
    public class Program
    {
        static void Main(string[] args)
        {
            string text = File.ReadAllText(args[0]);

            Parse.Parser.ParseText(text);

            /*
            // Create rule
            Rule rule = new Rule("Start game when there are at least 3 players.");

            // Set conditions
            rule.Conditions = new Condition[]
            {
                // If the game is not initialized (global variable I)
                new Condition(Element.Part<V_GlobalVariable>(Variable.I), Operators.Equal, new V_False()),

                // and there is at least 3 players.
                new Condition(new V_NumberOfPlayers(), Operators.GreaterThanOrEqual, new V_Number(3)),
            };

            // Set actions
            rule.Actions = new Element[]
            {
                // Send a message to chat containing the number of players.
                Element.Part<A_BigMessage>(new V_AllPlayers(), V_String.BuildString(new V_String("current players"), new V_NumberOfPlayers())),

                // Set initilized (global variable I) to true.
                Element.Part<A_SetGlobalVariable>(Variable.I, new V_True())
            };

            // Apply
            rule.Input();
            */

            Console.WriteLine("Done.");
            Console.ReadLine();
        }
    }
}
