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

            Elements.String.BuildString(new Elements.String("hello"), new Elements.String("you!"));

            Element BigAction = Element.Part<BigMessage>
            (
                Element.Part<Add>(new Number(2), new Number(4)),
                new OverwatchParser.Elements.String("...", 
                Element.Part<Add>(new Number(1), new Number(2)),
                Element.Part<Add>(new Number(1), new Number(2)),
                Element.Part<Add>(new Number(1), new Number(2))));

            BigAction.Input();

            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        public static InputHandler Input = new InputHandler(Process.GetProcessesByName("Overwatch")[0]);
    }
}
