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
using Deltin.Deltinteger.Decompiler.TextToElement;
using Deltin.Deltinteger.Decompiler.ElementToCode;
using TextCopy;
using System.Diagnostics.CodeAnalysis;
using Deltin.Deltinteger.LanguageServer.Model;
using Deltin.Deltinteger.Lobby2.Json;
using Deltin.Deltinteger.Lobby;

namespace Deltin.Deltinteger
{
    public class Program
    {
        public const string VERSION = "v2.8.7.1";

        public static readonly string ExeFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static string[] args;

        public static Log Log = new Log(":");

        static readonly ArgRunner[] ArgRunners = new ArgRunner[] {
            new RunPing(),
            new RunLanguageServer(),
            new RunGenerateLobbySchema(),
            new RunEditor(),
            new RunDecompileClipboard(),
            new RunDefault()
        };

        // These types have a default parameterless constructor that is called with reflection.
        // When building for 'browser-wasm', The constructors will be trimmed because the metadata and IL thinks that
        // they are never called. These attributes will prevent this behaviour.
        // Any type that is intended to be generated from json should be added as an attribute below.
        // Element.js types:
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ElementRoot))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ElementJsonValue))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ElementJsonAction))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ElementEnum))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ElementEnumMember))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ElementParameter))]
        // Lobby settings:
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SettingsSchemaJson))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SObject))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Template))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TemplateParam))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MapToOstw))]
        // Lobby settings (map):
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LobbyMap))]
        // Wasm interp types:
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InterpChangeEvent))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InterpRange))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InterpPosition))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InterpScriptDiagnostics))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InterpDiagnostic))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InterpCompletionItem))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InterpSignature))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InterpSignatureParameter))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InterpSignatureContext))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InterpSemantics))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InterpHover))]
        static void Main(string[] args)
        {
            if (args.ElementAtOrDefault(0) == "--ping")
            {
                Console.Write("Hello!");
                return;
            }

            if (args.Contains("--waitfordebugger") && !WaitForDebugger())
                return;

            Program.args = args;
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

            LoadData.LoadFromFileSystem();
            Lobby.HeroSettingCollection.Init();
            Lobby.ModeSettingCollection.Init();

            Log.LogLevel = LogLevel.Normal;
            if (args.Contains("-verbose"))
                Log.LogLevel = LogLevel.Verbose;
            if (args.Contains("-quiet"))
                Log.LogLevel = LogLevel.Quiet;

            foreach (var runner in ArgRunners)
            {
                runner.Args = args;
                if (runner.Run())
                    break;
            }
        }

        static bool WaitForDebugger()
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            while (!System.Diagnostics.Debugger.IsAttached)
            {
                Thread.Sleep(100);

                if (stopwatch.ElapsedMilliseconds > 30000)
                    return false;
            }
            return true;
        }

        public static void Script(string parseFile)
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

    abstract class ArgRunner
    {
        public string[] Args { get; set; }
        protected int CurrentArg { get; private set; }
        public abstract bool Run();
        protected bool IsArg(string arg) => Args.ElementAtOrDefault(CurrentArg) == arg;
        protected void NextArg()
        {
            CurrentArg++;
        }
        protected string GetCurrentArg() => Args.ElementAtOrDefault(CurrentArg);
    }

    class RunPing : ArgRunner
    {
        public override bool Run()
        {
            if (IsArg("--ping"))
            {
                Console.Write("Hello!");
                return true;
            }
            return false;
        }
    }

    class RunLanguageServer : ArgRunner
    {
        public override bool Run()
        {
            if (IsArg("--langserver"))
            {
                StdServer.Run();
                return true;
            }
            return false;
        }
    }

    class RunGenerateLobbySchema : ArgRunner
    {
        public override bool Run()
        {
            if (IsArg("--schema"))
            {
                Deltin.Deltinteger.Lobby.Ruleset.GenerateSchema();
                return true;
            }
            return false;
        }
    }

    class RunEditor : ArgRunner
    {
        public override bool Run()
        {
            if (IsArg("--editor"))
            {
                string pathfindEditorScript = Extras.CombinePathWithDotNotation(null, "!PathfindEditor.del");

                if (!File.Exists(pathfindEditorScript))
                    Program.Log.Write(LogLevel.Normal, "The PathfindEditor.del module is missing!");
                else
                    Program.Script(pathfindEditorScript);
                return true;
            }
            return false;
        }
    }

    class RunDecompileClipboard : ArgRunner
    {
        public override bool Run()
        {
            if (!IsArg("--decompile-clipboard")) return false;
            NextArg();
            string file = GetCurrentArg();

            try
            {
                // Parse the workshop code.
                var tte = new ConvertTextToElement(Clipboard.GetText());
                var workshop = tte.Get();

                // Decompile the parsed workshop code.
                var workshopToCode = new WorkshopDecompiler(workshop, new FileLobbySettingsResolver(file, workshop.LobbySettings), new CodeFormattingOptions());
                string result = workshopToCode.Decompile();

                // Create the file.
                using (var writer = File.CreateText(file))
                    // Write the code to the file.
                    writer.Write(result);

                Console.Write("Success");

                // Warning if the end of the file was not reached.
                if (!tte.ReachedEnd)
                    Console.Write("End of file not reached, stuck at: '" + tte.LocalStream.Substring(0, Math.Min(tte.LocalStream.Length, 50)) + "'");
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
            }

            // Done.
            return true;
        }
    }

    class RunDefault : ArgRunner
    {
        public override bool Run()
        {
            Program.Log.Write(LogLevel.Normal, "Overwatch Script To Workshop " + Program.VERSION);

            string script = Args.ElementAtOrDefault(0);

            if (script != null && File.Exists(script))
            {
#if DEBUG == false
                try
                {
#endif

                RunFile(script);

#if DEBUG == false
                }
                catch (Exception ex)
                {
                    Program.Log.Write(LogLevel.Normal, "Internal exception.");
                    Program.Log.Write(LogLevel.Normal, ex.ToString());
                }
#endif
                return true;
            }
            return false;
        }

        private void RunFile(string script)
        {
            string ext = Path.GetExtension(script).ToLower();
            // Run .csv file
            if (ext == ".csv")
            {
                Pathmap map = Pathmap.ImportFromActionSetFile(script, new ConsolePathmapErrorHandler(new Log("Pathmap")));
                if (map != null)
                {
                    string result = map.ExportAsJSON();
                    string output = Path.ChangeExtension(script, "pathmap");
                    using (FileStream fs = File.Create(output))
                    {
                        Byte[] info = Encoding.Unicode.GetBytes(result);
                        fs.Write(info, 0, info.Length);
                    }
                    Program.Log.Write(LogLevel.Normal, "Created pathmap file at '" + output + "'.");
                }
            }
            // Run .pathmap file
            else if (ext == ".pathmap")
            {
                Editor.FromPathmapFile(script);
            }
            // Decompile .ow file
            else if (ext == ".ow")
            {
                string text = File.ReadAllText(script);

                // Parse the workshop code.
                var walker = new ConvertTextToElement(text);
                var workshop = walker.Get();

                // Decompile to OSTW.
                var decompiler = new WorkshopDecompiler(workshop, new OmitLobbySettingsResolver(), new CodeFormattingOptions());

                // Result
                Program.WorkshopCodeResult(decompiler.Decompile());
            }
            // Default: Run OSTW script
            else
                Program.Script(script);
        }
    }
}
