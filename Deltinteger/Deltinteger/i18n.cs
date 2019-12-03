using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.I18n
{
    public class GenerateI18n
    {
        static readonly string[] Languages = new string[] {
            "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW"
        };
        static readonly string[] ProcLanguages = new string[] {
            "deDE", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW"
        };

        static readonly Log Log = new Log("i18n");
        static readonly string syntax = "deltinteger i18n \"datatool file location\" \"[overwatch file location]\"";

        public static void Generate(string[] args)
        {
            string datatoolPath = args.ElementAtOrDefault(1);
            string overwatchPath = args.ElementAtOrDefault(2);

            if (datatoolPath == null)
            {
                Log.Write(LogLevel.Normal, syntax);
                return;
            }

            if (overwatchPath == null)
            {
                overwatchPath = "C:/Program Files (x86)/Overwatch";

                if (!Directory.Exists(overwatchPath))
                {
                    Log.Write(LogLevel.Normal, "Could not find a folder at the default Overwatch install location.");
                    Log.Write(LogLevel.Normal, syntax);
                    return;
                }
            }
            else if (!Directory.Exists(overwatchPath))
            {
                Log.Write(LogLevel.Normal, "Could not find a folder at " + overwatchPath + ".");
                Log.Write(LogLevel.Normal, syntax);
                return;
            }

            new GenerateI18n(datatoolPath, overwatchPath);
        }

        readonly string datatoolPath;
        readonly string overwatchPath;

        public GenerateI18n(string datatoolPath, string overwatchPath)
        {
            this.datatoolPath = datatoolPath;
            this.overwatchPath = overwatchPath;

            var engKeys = CreateKeysFromDump(DumpStrings("enUS"));
            Dictionary<string, Dictionary<string, string>> languages = new Dictionary<string, Dictionary<string, string>>();

            foreach (var lang in ProcLanguages)
            {
                Log.Write(LogLevel.Normal, "Exporting " + lang + "...");
                languages.Add(lang, CreateKeysFromDump(DumpStrings(lang)));
            }
        }

        string RunCommand(string arguments)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = datatoolPath;
            startInfo.Arguments = string.Format("\"{0}\" {1}", overwatchPath, arguments);
            startInfo.WorkingDirectory = Path.GetDirectoryName(datatoolPath);
            
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;

            string output;

            using (Process dataToolProcess = Process.Start(startInfo))
            {
                output = dataToolProcess.StandardOutput.ReadToEnd();
                dataToolProcess.WaitForExit();
            }

            return output;
        }

        string DumpStrings(string language)
        {
            if (language == null) throw new ArgumentNullException(nameof(language));
            if (!Languages.Contains(language)) throw new ArgumentException(language + " is not a valid language.", nameof(language));
            return RunCommand("dump-strings --language=" + language);
        }

        Dictionary<string, string> CreateKeysFromDump(string stringDump)
        {
            var keys = new Dictionary<string, string>();

            string[] lines = stringDump.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            foreach (var line in lines)
            {
                string[] lineSplit = line.Split(':', 2);
                if (lineSplit.Length == 2)
                {
                    string key = lineSplit[0];
                    if (!keys.ContainsKey(key))
                    {
                        string str = lineSplit[1].Substring(1);
                        keys.Add(key, str);
                    }
                }
            }

            return keys;
        }
    }
}