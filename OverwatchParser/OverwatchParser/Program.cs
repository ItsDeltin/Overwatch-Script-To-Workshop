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
            StringParser.ParseString("ability: crouch near dead player -> find team (<0> left)", new Element[]
            {
                Element.Part<V_GlobalVariable>(Variable.A)
            });
            Console.WriteLine("Done.");
            Console.ReadLine();

            string text = File.ReadAllText(args[0]);

            Rule[] generatedRules = Parser.ParseText(text);

            Console.ReadLine();

            if (generatedRules != null)
                foreach (Rule rule in generatedRules)
                    rule.Input();

            Console.WriteLine("Done.");
            Console.ReadLine();
        }
    }
}
