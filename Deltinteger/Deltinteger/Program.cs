using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Checker;

namespace Deltin.Deltinteger
{
    public class Program
    {
        static Log Log = new Log(":");

        static void Main(string[] args)
        {
            Log.LogLevel = LogLevel.Normal;
            if (args.Contains("-verbose"))
                Log.LogLevel = LogLevel.Verbose;
            if (args.Contains("-quiet"))
                Log.LogLevel = LogLevel.Quiet;

            if (args.Contains("-langserver"))
            {
                int.TryParse(args.FirstOrDefault(v => v.Split(' ')[0] == "-port").Split(' ')[1], out int port);
                Check.RequestLoop(port);
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
                else if (script != null)
                {
                    Log.Write(LogLevel.Normal, $"Could not find the file \"{script}\"");
                }
                else
                {
                    Log.Write(LogLevel.Normal, $"Drag and drop a script over the executable to parse.");
                    ConsoleLoop.Start();
                }

                Log.Write(LogLevel.Normal, "Done. Press enter to exit.");
                Console.ReadLine();
            }
        }

        static void Script(string parseFile)
        {
            string text = File.ReadAllText(parseFile);

            Rule[] generatedRules = null;
#if DEBUG == false
            try
            {
                generatedRules = Parser.ParseText(text, out _);
            }
            catch (SyntaxErrorException ex)
            {
                Log.Write(LogLevel.Normal, new ColorMod(ex.Message, ConsoleColor.Red));
                return;
            }
#else
            generatedRules = Parser.ParseText(text, out _);
#endif

            string final = RuleArrayToWorkshop(generatedRules);

            Log.Write(LogLevel.Normal, "Press enter to copy code to clipboard, then in Overwatch click \"Paste Rule\".");
            Console.ReadLine();

            InputSim.SetClipboard(final);
        }

        public static string RuleArrayToWorkshop(Rule[] rules)
        {
            var builder = new StringBuilder();
            Log debugPrintLog = new Log("Tree");
            foreach (var rule in rules)
            {
                rule.DebugPrint(debugPrintLog);
                builder.AppendLine(rule.ToWorkshop());
                builder.AppendLine();
            }
            return builder.ToString();
        }
    }
}
