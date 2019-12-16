using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;

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
        static readonly string syntax = "deltinteger i18n \"datatool file location\" language \"output file\" \"[overwatch file location]\" ";

        public static void Generate(string[] args)
        {
            string datatoolPath  = args.ElementAtOrDefault(1);
            string lang          = args.ElementAtOrDefault(2);
            string outputFile    = args.ElementAtOrDefault(3);
            string overwatchPath = args.ElementAtOrDefault(4);

            // Return if one of the required arguments is missing.
            if (datatoolPath == null || lang == null || outputFile == null)
            {
                Log.Write(LogLevel.Normal, syntax);
                return;
            }

            // Return if the language parameter isn't a valid language.
            if (!ProcLanguages.Contains(lang))
            {
                Log.Write(LogLevel.Normal, $"'{lang}' is not a valid language. The options are {string.Join(", ", ProcLanguages)}.");
                Log.Write(LogLevel.Normal, syntax);
                return;
            }

            // Get the overwatch path.
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

            new GenerateI18n(datatoolPath, lang, outputFile, overwatchPath);
        }

        readonly string datatoolPath;
        readonly string overwatchPath;

        private GenerateI18n(string datatoolPath, string lang, string directory, string overwatchPath)
        {
            this.datatoolPath = datatoolPath;
            this.overwatchPath = overwatchPath;

            Log.Write(LogLevel.Normal, "Getting enUS keys...");
            var engKeys = CreateKeysFromDump(DumpStrings("enUS"));
            Log.Write(LogLevel.Normal, "Got enUS keys.");

            Log.Write(LogLevel.Normal, $"Getting {lang} keys...");
            var languageKeys = CreateKeysFromDump(DumpStrings(lang));
            Log.Write(LogLevel.Normal, $"Got {lang} keys.");

            XmlSerializer serializer = new XmlSerializer(typeof(I18nLanguage));
            I18nLanguage xml = new I18nLanguage(Log, engKeys, languageKeys);

            serializer = new XmlSerializer(typeof(I18nLanguage));

            string file = Path.Combine(directory, "i18n-" + lang + ".xml");

            if (File.Exists(file))
                File.Delete(file);
            
            using (var fileStream = File.Create(file))
            using (StreamWriter writer = new StreamWriter(fileStream))
                serializer.Serialize(writer, xml);
            
            Log.Write(LogLevel.Normal, "Finished.");
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

        static StringKeyGroup CreateKeysFromDump(string stringDump)
        {
            StringKeyGroup keys = new StringKeyGroup();

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
                    if (!keys.ContainsKey(key) && lineSplit[1].Length > 0)
                    {
                        string str = lineSplit[1].Substring(1);
                        keys.Add(key, str);
                    }
                }
            }

            return keys;
        }
    }

    public class StringKeyGroup
    {
        private readonly List<StringKey> _keys = new List<StringKey>();

        public void Add(StringKey key)
        {
            _keys.Add(key);
        }
        public void Add(string key, string value)
        {
            Add(new StringKey(key, value));
        }

        public StringKey FromKey(string key) => _keys.FirstOrDefault(k => k.Key.ToLower() == key.ToLower());
        public StringKey FromValue(string value) => _keys.FirstOrDefault(k => k.Value.ToLower() == value.ToLower());
        public bool ContainsKey(string key) => _keys.Any(k => k.Key == key);
    }
    
    public class StringKey
    {
        public string Key { get; }
        public string Value { get; }

        public StringKey(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}