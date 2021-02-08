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

        public string DatatoolPath { get; }
        public string OverwatchPath { get; }

        public DataTool(string datatoolPath, string overwatchPath)
        {
            DatatoolPath = datatoolPath;
            OverwatchPath = overwatchPath;
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
            startInfo.Arguments = $"/C datatool \"{OverwatchPath}\" {arguments} > {outName}";
            startInfo.WorkingDirectory = Path.GetDirectoryName(DatatoolPath);
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            using (Process dataToolProcess = Process.Start(startInfo))
                dataToolProcess.WaitForExit();

            string outFile = Path.Join(Path.GetDirectoryName(DatatoolPath), outName);
            return File.ReadAllText(outFile);
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
    }
}