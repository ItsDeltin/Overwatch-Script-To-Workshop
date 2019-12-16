using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
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
        readonly string lang;
        readonly string file;
        readonly string overwatchPath;

        public GenerateI18n(string datatoolPath, string lang, string file, string overwatchPath)
        {
            this.datatoolPath = datatoolPath;
            this.lang = lang;
            this.file = file;
            this.overwatchPath = overwatchPath;

            XmlSerializer serializer = new XmlSerializer(typeof(WorkshopI18n));
            WorkshopI18n xml;

            if (File.Exists(file))
            {
                using (FileStream fileStream = File.OpenRead(file))
                    xml = (WorkshopI18n)serializer.Deserialize(fileStream);
                
                xml.AddMissing(this);
            }
            else
            {
                var engKeys = EnglishKeys();

                xml = new WorkshopI18n();
                xml.AddMissing(this);
                foreach (var method in xml.Methods)
                {
                    var key = engKeys.FirstOrDefault(engKey => engKey.Value.ToLower() == method.EnglishName.ToLower());
                    method.Key = key.Key;
                    if (key.Key == null)
                        Log.Write(LogLevel.Normal, $"Couldn't find the key for the value '{method.EnglishName}'.");
                }
            }

            Log.Write(LogLevel.Normal, $"Getting {lang} keys...");
            var languageKeys = CreateKeysFromDump(DumpStrings(lang));
            Log.Write(LogLevel.Normal, $"Got {lang} keys.");

            foreach (var method in xml.Methods)
            {
                method.Translations.RemoveAll(t => t.Language == lang);
                method.Translations.Add(new WorkshopI18n.WorkshopMethod.LanguageName(lang, languageKeys[method.Key]));
            }

            // Add the language to the language list if it isn't added yet.
            if (!xml.Languages.Contains(lang)) xml.Languages.Add(lang);

            serializer = new XmlSerializer(typeof(WorkshopI18n));
            using (StreamWriter writer = new StreamWriter(file))
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

        static Dictionary<string, string> CreateKeysFromDump(string stringDump)
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
                    if (!keys.ContainsKey(key) && lineSplit[1].Length > 0)
                    {
                        string str = lineSplit[1].Substring(1);
                        keys.Add(key, str);
                    }
                }
            }

            return keys;
        }

        private Dictionary<string, string> _englishKeys;
        public Dictionary<string, string> EnglishKeys()
        {
            if (_englishKeys == null)
            {
                Log.Write(LogLevel.Normal, "Getting english keys...");
                _englishKeys = CreateKeysFromDump(DumpStrings("enUS"));
                Log.Write(LogLevel.Normal, "Got english keys.");
            }
            return _englishKeys;
        }
    }

    public class WorkshopI18n
    {
        public WorkshopI18n()
        {
        }

        public void AddMissing(GenerateI18n i18n)
        {
            for (int i = 0; i < ElementList.Elements.Length; i++)
                if (!Methods.Any(m => m.EnglishName.ToLower() == ElementList.Elements[i].WorkshopName.ToLower()))
                {
                    Methods.Add(new WorkshopMethod(ElementList.Elements[i].WorkshopName) {
                        Key = i18n.EnglishKeys().First(key => key.Value.ToLower() == ElementList.Elements[i].WorkshopName.ToLower()).Key
                    });
                }
        }

        [XmlArrayItem("language")]
        public List<string> Languages { get; } = new List<string>();
        [XmlArrayItem("method")]
        public List<WorkshopMethod> Methods { get; } = new List<WorkshopMethod>();

        public class WorkshopMethod
        {
            public WorkshopMethod() {}
            public WorkshopMethod(string englishName)
            {
                EnglishName = englishName;
            }

            [XmlAttribute]
            public string EnglishName { get; set; }
            [XmlAttribute]
            public string Key { get; set; }
            [XmlElement("lang")]
            public List<LanguageName> Translations { get; } = new List<LanguageName>();

            public class LanguageName
            {
                public LanguageName() {}
                public LanguageName(string language, string name)
                {
                    Language = language;
                    Name = name;
                }

                [XmlAttribute("id")]
                public string Language { get; set; }
                [XmlAttribute]
                public string Name { get; set; }
            }
        }
    }
}