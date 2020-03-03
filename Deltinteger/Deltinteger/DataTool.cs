using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace Deltin.Deltinteger.Dump
{
    public class DataTool
    {
        public static readonly string[] Languages = new string[] {
            "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW"
        };

        readonly string datatoolPath;
        readonly string overwatchPath;

        public DataTool(string datatoolPath, string overwatchPath)
        {
            this.datatoolPath = datatoolPath;
            this.overwatchPath = overwatchPath;
        }

        /// <summary>Runs a command in DataTool.exe.</summary>
        /// <param name="arguments">The arguments of the command.</param>
        /// <param name="outName">The name of the output.</param>
        /// <returns>The output of the command.</returns>
        public string RunCommand(string arguments, string outName)
        {
            outName = $"out_{outName}.txt";

            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/C datatool \"{overwatchPath}\" {arguments} > {outName}";
            startInfo.WorkingDirectory = Path.GetDirectoryName(datatoolPath);
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            using (Process dataToolProcess = Process.Start(startInfo))
                dataToolProcess.WaitForExit();

            string outFile = Path.Join(Path.GetDirectoryName(datatoolPath), outName);
            return File.ReadAllText(outFile);
        }

        public string RunCommandStd(string arguments)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/C datatool \"{overwatchPath}\" {arguments}";
            startInfo.WorkingDirectory = Path.GetDirectoryName(datatoolPath);
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            using (Process dataToolProcess = Process.Start(startInfo))
            {
                dataToolProcess.WaitForExit();
                return dataToolProcess.StandardOutput.ReadToEnd();
            }
        }

        /// <summary>Dumps strings for a language.</summary>
        /// <param name="language">The language to get the strings of.</param>
        public string DumpStrings(string language)
        {
            // Check the 'language' argument.
            if (language == null) throw new ArgumentNullException(nameof(language));
            if (!Languages.Contains(language)) throw new ArgumentException(language + " is not a valid language.", nameof(language));

            return RunCommand("dump-strings --language=" + language, "strings_" + language);
        }

        /// <summary>Dumps game settings.</summary>
        public string GetRulesets(string language = null)
        {
            if (language == null) return RunCommandStd("list-game-rulesets --json");
            else return RunCommandStd("list-game-rulesets --json --language=" + language);
        }
    }
}