using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Globalization;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Pathfinder;
using TextCopy;

namespace Deltin.Deltinteger
{
    public class Program
    {
        public const string VERSION = "v1.2";

        public static readonly string ExeFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static string[] args;

        static Log Log = new Log(":");
        static Log ParseLog = new Log("Parse");


        static void Main(string[] args)
        {
            Program.args = args;
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            Lobby.HeroSettingCollection.Init();
            Lobby.ModeSettingCollection.Init();

            if (!args.Contains("--langserver")) Log.Write(LogLevel.Normal, "Overwatch Script To Workshop " + VERSION);

            Log.LogLevel = LogLevel.Normal;
            if (args.Contains("-verbose"))
                Log.LogLevel = LogLevel.Verbose;
            if (args.Contains("-quiet"))
                Log.LogLevel = LogLevel.Quiet;

            if (args.Contains("--langserver"))
            {
                Log.LogLevel = LogLevel.Quiet;
                DeltintegerLanguageServer.Run();
            }
            else if (args.Contains("--generatealphabet"))
            {
                Console.Write("Output folder: ");
                string folder = Console.ReadLine();
                Deltin.Deltinteger.Models.Letter.Generate(folder);
            }
            else if (args.Contains("--editor"))
            {
                string pathfindEditorScript = Extras.CombinePathWithDotNotation(null, "!PathfindEditor.del");

                if (!File.Exists(pathfindEditorScript))
                    Log.Write(LogLevel.Normal, "The PathfindEditor.del module is missing!");
                else
                    Script(pathfindEditorScript);
            }
            else if (args.ElementAtOrDefault(0) == "--i18n") I18n.GenerateI18n.Generate(args);
            else if (args.ElementAtOrDefault(0) == "--i18nlink") I18n.GenerateI18n.GenerateKeyLink();
            else if (args.ElementAtOrDefault(0) == "--wiki")
            {
                var wiki = WorkshopWiki.Wiki.GetWiki();
                if (wiki != null)
                {
                    Console.Write("Output file: ");
                    string outputPath = Console.ReadLine();
                    wiki.ToXML(outputPath);
                }
            }
            else if (args.ElementAtOrDefault(0) == "--schema") Deltin.Deltinteger.Lobby.Ruleset.GenerateSchema();
            else if (args.ElementAtOrDefault(0) == "--maps") Deltin.Deltinteger.Lobby.LobbyMap.GetMaps(args[1], args[2], args[3]);
            else
            {
                string script = args.ElementAtOrDefault(0);

                if (script != null && File.Exists(script))
                {
                    #if DEBUG == false
                    try
                    {
                    #endif

                        string ext = Path.GetExtension(script).ToLower();
                        if (ext == ".csv")
                        {
                            PathMap map = PathMap.ImportFromCSV(script);
                            string result = map.ExportAsXML();
                            string output = Path.ChangeExtension(script, "pathmap");
                            using (FileStream fs = File.Create(output))
                            {
                                Byte[] info = Encoding.Unicode.GetBytes(result);
                                fs.Write(info, 0, info.Length);
                            }
                            Log.Write(LogLevel.Normal, "Created pathmap file at '" + output + "'.");
                        }
                        else if (ext == ".pathmap")
                        {
                            Editor.FromPathmapFile(script);
                        }
                        else
                            Script(script);
                    
                    #if DEBUG == false
                    }
                    catch (Exception ex)
                    {
                        Log.Write(LogLevel.Normal, "Internal exception.");
                        Log.Write(LogLevel.Normal, ex.ToString());
                    }
                    #endif
                }
                else
                {
                    Log.Write(LogLevel.Normal, $"Could not find the file '{script}'.");
                    Log.Write(LogLevel.Normal, $"Drag and drop a script over the executable to parse.");
                }
            }
            
            Finished();
        }

        static void Script(string parseFile)
        {
            string text = File.ReadAllText(parseFile);

            Diagnostics diagnostics = new Diagnostics();
            ScriptFile root = new ScriptFile(diagnostics, new Uri(parseFile), text);
            DeltinScript deltinScript = new DeltinScript(new TranslateSettings(diagnostics, root));
            diagnostics.PrintDiagnostics(Log);
            if (deltinScript.WorkshopCode != null)
                WorkshopCodeResult(deltinScript.WorkshopCode);
        }

        public static void WorkshopCodeResult(string code)
        {
            Log.Write(LogLevel.Normal, "Press enter to copy code to clipboard, then in Overwatch click \"Paste Rule\".");
            Console.ReadLine();
            Clipboard.SetText(code);
        }

        public static void Finished()
        {
            Log.Write(LogLevel.Normal, "Done. Press enter to exit.");
            Console.ReadLine();
        }
    }
}
