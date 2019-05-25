using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger
{
    public class Program
    {
        static Log Log = new Log(":");

        static void Main(string[] args)
        {
            string script = args.ElementAtOrDefault(0);
            Log.LogLevel = args.ElementAtOrDefault(1) == "-verbose" ? LogLevel.Verbose : LogLevel.Normal;

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

        static void Script(string parseFile)
        {
            string text = File.ReadAllText(parseFile);

            Rule[] generatedRules = null;
#if DEBUG == false
            try
            {
                generatedRules = Parser.ParseText(text);
            }
            catch (SyntaxErrorException ex)
            {
                Log.Write(LogLevel.Normal, new ColorMod(ex.Message, ConsoleColor.Red));
                return;
            }
#else
            generatedRules = Parser.ParseText(text);
#endif

            var builder = new StringBuilder();
            foreach (var rule in generatedRules)
            {
                builder.AppendLine(rule.ToWorkshop());
                builder.AppendLine();
            }
            string final = builder.ToString();

            Log.Write(LogLevel.Normal, "Press enter to copy code to clipboard, then in Overwatch click \"Paste Rule\".");
            Console.ReadLine();

            InputSim.SetClipboard(final);
        }
    }
}
