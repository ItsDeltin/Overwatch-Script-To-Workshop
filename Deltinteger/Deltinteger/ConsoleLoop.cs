using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger
{
    static class ConsoleLoop
    {
        private static readonly string[] Help = new string[]
        {
            "    syntax <name>        Gets the syntax of a method.",
            "    list                 Lists all actions and values.",
            "    list actions         Lists all actions.",
            "    list values          Lists all values.",
            "    list enums           List all enums.",
            "    autocomplete npp     Generates an autocomplete file that can be used with Notepad++.",
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

                        var enumParameter = Constants.EnumParameters.Where(v => v.Name == method).FirstOrDefault();
                        if (enumParameter != null)
                        {
                            ListEnum(enumParameter);
                            break;
                        }

                        Console.WriteLine("Unknown method.");
                        break;

                    case "autocomplete":
                        switch (inputSplit.ElementAtOrDefault(1)?.ToLower())
                        {
                            case "npp":
                                AutoCompleteNPP();
                                break;

                            default:
                                ListHelp();
                                break;
                        }
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
                ListEnum(e);
        }

        static void ListEnum(Type @enum)
        {
            Console.WriteLine($"{@enum.Name}");
            string[] values = @enum.GetEnumNames();
            foreach (string value in values)
                Console.WriteLine($"    {value}");
        }

        static void ListCustomMethods()
        {
            Console.Write("Other:");
            Console.WriteLine(string.Join("", CustomMethods.CustomMethodList.Select(v => $"\n    {CustomMethods.GetName(v)}")));
        }

        static void AutoCompleteNPP()
        {
            var doc = new XDocument(new XElement("NotepadPlus",
                new XElement("AutoComplete", new XAttribute("language", "workshop"),
                    new XElement("Enviroment", new XAttribute("ignoreCase", "no"), new XAttribute("startFunc", "("), new XAttribute("stopFunc", ")"), new XAttribute("paramSeperator", ","), new XAttribute("terminal", ";"), new XAttribute("additionalWordChar", ".")),
                    Element.MethodList.Select(type =>
                    {
                        string methodName = type.Name.Substring(2);
                        ElementData methodData = type.GetCustomAttribute<ElementData>();
                        Parameter[] parameters = type.GetCustomAttributes<Parameter>().ToArray();

                        return new XElement("KeyWord", new XAttribute("name", methodName), new XAttribute("func", "yes"),
                            new XElement("Overload", new XAttribute("retVal", methodData.ValueType.ToString()), new XAttribute("descr", ""),
                                parameters.Select(param =>
                                    new XElement("Param", new XAttribute("name", $"{(param.ParameterType == ParameterType.Value ? param.ValueType.ToString() : param.EnumType.ToString())}: {param.Name}"))
                                )
                            )
                        );
                    }).OrderBy(v => v.Attribute("name").Value)
                )
            )); //.Save(Path.Combine(Constants.WorkingDirectory, "workshop.xml"));

            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "\t"
            };
            using (XmlWriter xw = XmlWriter.Create(Path.Combine(Constants.WorkingDirectory, "workshop.xml"), settings))
            {
                doc.Save(xw);
            }
        }
    }
}
