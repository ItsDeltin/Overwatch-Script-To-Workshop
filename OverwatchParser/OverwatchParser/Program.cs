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
            Element.LoadAllElements();

            Rule rule = new Rule("Rule rocks!")
            {
                Conditions = new Condition[]
                {
                    new Condition(new V_Number(1))
                }
            };
            rule.Input();

            Console.WriteLine("Done.");
            Console.ReadLine();
        }
    }
}
