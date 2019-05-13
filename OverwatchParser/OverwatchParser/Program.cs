using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using OverwatchParser.Elements;

namespace OverwatchParser
{
    public class Program
    {
        static void Main(string[] args)
        {
            Element.LoadAllElements();

            /*
            new OverwatchParser.Elements.AbortIf()
            {
                ParameterValues = new Element[1]
                {
                    new And()
                    {
                        ParameterValues = new Element[2]
                        {
                            new AppendToArray()
                            {
                                ParameterValues = new Element[2]
                                {
                                    new AbsoluteValue(new Number(-30)),
                                    new AbsoluteValue()
                                    {
                                        ParameterValues = new Element[1]
                                        {
                                            new Number(20)
                                        }
                                    }
                                }
                            },
                            new True()
                        }
                    }
                }
            }.Input();
            */

            Element action = Element.Part<AbortIf>(
                Element.Part<And>(
                    Element.Part<AppendToArray>(
                        Element.Part<AbsoluteValue>(new Number(-30)),
                        Element.Part<AbsoluteValue>(new Number(20))
                        ), Element.Part<True>()));
            action.Input();

            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        public static InputHandler Input = new InputHandler(Process.GetProcessesByName("Overwatch")[0]);
    }
}
