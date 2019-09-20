using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using System.Globalization;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Pathfinder;

namespace Deltin.Deltinteger
{
    public class Program
    {
        public const string VERSION = "v0.5.0.1";

        public static readonly string ExeFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        static Log Log = new Log(":");
        static Log ParseLog = new Log("Parse");


        static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            Log.Write(LogLevel.Normal, "Overwatch Script To Workshop " + VERSION);

            Log.LogLevel = LogLevel.Normal;
            if (args.Contains("-verbose"))
                Log.LogLevel = LogLevel.Verbose;
            if (args.Contains("-quiet"))
                Log.LogLevel = LogLevel.Quiet;

            if (args.Contains("-langserver"))
            {
                string[] portArgs = args.FirstOrDefault(v => v.Split(' ')[0] == "-port")?.Split(' ');
                int.TryParse(portArgs.ElementAtOrDefault(1), out int serverPort);
                new Server().RequestLoop(serverPort);
            }
            else if (args.Contains("-pathfind"))
            {
                PathMap map = PathMap.ImportFromCSV(@"C:\Users\HDdel\Desktop\csv.txt");
                map.Export();
            }
            else if (args.Contains("-generatealphabet"))
            {
                Console.Write("Output folder: ");
                string folder = Console.ReadLine();
                Deltin.Deltinteger.Models.Letter.Generate(folder);
            }
            else
            {
                string script = args.ElementAtOrDefault(0);

                if (File.Exists(script))
                {
                    # if DEBUG == false
                    try
                    {
                        Script(script);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(LogLevel.Normal, "Internal exception.");
                        Log.Write(LogLevel.Normal, ex.ToString());
                    }
                    #else
                    Script(script);
                    #endif
                }
                else
                {
                    Log.Write(LogLevel.Normal, $"Could not find the file '{script}'.");
                    Log.Write(LogLevel.Normal, $"Drag and drop a script over the executable to parse.");
                }

                Log.Write(LogLevel.Normal, "Done. Press enter to exit.");
                Console.ReadLine();
            }
        }

        static void Script(string parseFile)
        {
            string text = File.ReadAllText(parseFile);

            ParsingData result = ParsingData.GetParser(parseFile, text);

            if (!result.Diagnostics.ContainsErrors())
            {
                ParseLog.Write(LogLevel.Normal, new ColorMod("Build succeeded.", ConsoleColor.Green));

                result.Diagnostics.PrintDiagnostics(Log);

                // List all variables
                ParseLog.Write(LogLevel.Normal, new ColorMod("Variable Guide:", ConsoleColor.Blue));

                if (result.VarCollection.AllVars.Count > 0)
                {
                    int nameLength = result.VarCollection.AllVars.Max(v => v.Name.Length);

                    bool other = false;
                    foreach (Var var in result.VarCollection.AllVars)
                    {
                        ConsoleColor textcolor = other ? ConsoleColor.White : ConsoleColor.DarkGray;
                        other = !other;

                        ParseLog.Write(LogLevel.Normal, new ColorMod(var.ToString(), textcolor));
                    }
                }

                string final = RuleArrayToWorkshop(result.Rules.ToArray(), result.VarCollection);

                Log.Write(LogLevel.Normal, "Press enter to copy code to clipboard, then in Overwatch click \"Paste Rule\".");
                Console.ReadLine();

                SetClipboard(final);
            }
            else
            {
                Log.Write(LogLevel.Normal, new ColorMod("Build Failed.", ConsoleColor.Red));
                result.Diagnostics.PrintDiagnostics(Log);
            }
        }

        public static string RuleArrayToWorkshop(Rule[] rules, VarCollection varCollection)
        {
            var builder = new StringBuilder();

            builder.AppendLine("// --- Variable Guide ---");

            foreach(var var in varCollection.AllVars)
                builder.AppendLine("// " + var.ToString());
            
            builder.AppendLine();

            Log debugPrintLog = new Log("Tree");
            foreach (var rule in rules)
            {
                rule.DebugPrint(debugPrintLog);
                builder.AppendLine(rule.ToWorkshop());
                builder.AppendLine();
            }
            return builder.ToString();
        }

        public static void SetClipboard(string text)
        {
            Thread setClipboardThread = new Thread(() => Clipboard.SetText(text));
            setClipboardThread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            setClipboardThread.Start();
            setClipboardThread.Join();
        }
    }
}
