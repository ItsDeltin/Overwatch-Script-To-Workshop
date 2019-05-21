using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger
{
    static class ConsoleLoop
    {
        private static readonly string[] Help = new string[]
        {
            "    syntax <name>  Gets the syntax of a method.",
            "    list           Lists all actions and values.",
            "    list actions   Lists all actions.",
            "    list values    Lists all values.",
            "    list enums     List all enums.",
            "    fixinput       Run this if Overwatch can't be interacted with after generation."
        };

        public static void Start()
        {
            Console.WriteLine("Commands:");
            ListHelp();
            while (true)
            {
                Console.Write(">");
                string input = Console.ReadLine();
                string[] inputSplit = input.Split(' ');

                switch (inputSplit[0].ToLower())
                {
                    case "help":
                        goto default;

                    case "list":
                        switch (inputSplit.ElementAtOrDefault(1)?.ToLower())
                        {
                            case null:
                                ListActions();
                                Console.WriteLine();
                                ListValues();
                                Console.WriteLine();
                                ListEnums();
                                Console.WriteLine();
                                ListCustomMethods();
                                break;

                            case "actions":
                                ListActions();
                                break;

                            case "values":
                                ListValues();
                                break;

                            case "enums":
                                ListEnums();
                                break;

                            default:
                                ListHelp();
                                break;
                        }
                        break;

                    case "syntax":
                        string method = inputSplit.ElementAtOrDefault(1);
                        if (method == null)
                            goto default;

                        Type element = Element.GetMethod(method);
                        if (element != null)
                        {
                            Console.WriteLine(Element.GetName(element));
                            break;
                        }

                        var customMethod = CustomMethods.GetCustomMethod(method);
                        if (customMethod != null)
                        {
                            Console.WriteLine(CustomMethods.GetName(customMethod));
                            break;
                        }

                        Console.WriteLine("Unknown method.");
                        break;

                    case "fixinput":
                        Process[] processes = Process.GetProcessesByName("Overwatch");
                        foreach (Process overwatch in processes)
                            User32.EnableWindow(overwatch.MainWindowHandle, true);

                        break;

                    default:
                        ListHelp();
                        break;
                }
            }
        }

        static void ListHelp()
        {
            Console.WriteLine(string.Join("\n", Help));
        }

        static void ListActions()
        {
            Console.Write("Actions:");
            Console.WriteLine(string.Join("", Element.ActionList.Select(v => $"\n    {Element.GetName(v)}")));
        }

        static void ListValues()
        {
            Console.Write("Values:");
            Console.WriteLine(string.Join("", Element.ValueList.Select(v => $"\n    {Element.GetName(v)}")));
        }

        static void ListEnums()
        {
            Console.WriteLine("Enums:");
            var enumParameters = Constants.EnumParameters;
            foreach(var e in enumParameters)
            {
                Console.WriteLine($"    {e.Name}");
                string[] values = e.GetEnumNames();
                foreach (string value in values)
                    Console.WriteLine($"        {value}");
            }
        }

        static void ListCustomMethods()
        {
            Console.Write("Other:");
            Console.WriteLine(string.Join("", CustomMethods.CustomMethodList.Select(v => $"\n    {CustomMethods.GetName(v)}")));
        }
    }
}
