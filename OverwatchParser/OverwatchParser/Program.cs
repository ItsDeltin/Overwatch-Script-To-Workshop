using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Deltin.OverwatchParser;
using Deltin.OverwatchParser.Elements;

namespace Deltin.OverwatchParser
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Create rule
            Rule rule = new Rule("Start game when there are at least 3 players.");

            // Set conditions
            rule.Conditions = new Condition[]
            {
                new Condition(Element.Part<V_GlobalVariable>(Variable.I), Operators.Equal,              new V_False()),
                new Condition(new V_NumberOfPlayers(),                    Operators.GreaterThanOrEqual, new V_Number(3)),
            };

            // Set actions
            rule.Actions = new Element[]
            {
                Element.Part<A_BigMessage>(new V_AllPlayers(), V_String.BuildString(new V_String("current players"), new V_NumberOfPlayers())),
            };

            // Apply
            rule.Input();

            Console.WriteLine("Done.");
            Console.ReadLine();
        }
    }
}
