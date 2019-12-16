using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Text;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.I18n
{
    public class GenerateI18n
    {
        static readonly Log Log = new Log("i18n");
        static readonly string syntax = "deltinteger i18n \"datatool file location\" language \"output file\" \"[overwatch file location]\"";

        static readonly string[] ProcLanguages = new string[] {
            "deDE", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW"
        };

        public static void Generate(string[] args)
        {
            string datatoolPath  = args.ElementAtOrDefault(1);
            string keyLinkFile   = args.ElementAtOrDefault(2);
            string outputFile    = args.ElementAtOrDefault(3);
            string overwatchPath = args.ElementAtOrDefault(4);

            // Return if one of the required arguments is missing.
            if (datatoolPath == null || outputFile == null)
            {
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

            var datatool = new DataTool(datatoolPath, overwatchPath);

            XmlSerializer linkSerializer = new XmlSerializer(typeof(KeyLinkList));
            KeyLink[] keyLinks;
            using (var fileStream = File.OpenRead(keyLinkFile))
                keyLinks = ((KeyLinkList)linkSerializer.Deserialize(fileStream)).Methods;

            XmlSerializer serializer = new XmlSerializer(typeof(I18nLanguage));
            foreach (string lang in ProcLanguages)
            {
                var languageKeys = datatool.KeysFromLang(Log, lang);
                I18nLanguage xml = new I18nLanguage();

                foreach (var element in ElementList.Elements)
                    xml.Methods.Add(new I18nMethod(
                        element.WorkshopName,
                        languageKeys.FromKey(keyLinks.First(m => m.MethodName == element.WorkshopName).Key).Value
                    ));

                string file = Path.Combine(outputFile, "i18n-" + lang + ".xml");

                if (File.Exists(file))
                    File.Delete(file);
                
                using (var fileStream = File.Create(file))
                using (StreamWriter writer = new StreamWriter(fileStream))
                    serializer.Serialize(writer, xml);
                
                Log.Write(LogLevel.Normal, "Finished " + lang + ".");
            }
        }

        public static void GenerateKeyLink()
        {
            string datatoolPath = "C:/Users/Deltin/Downloads/toolchain-release/DataTool.exe";
            string overwatchPath = "C:/Program Files (x86)/Overwatch";
            DataTool datatool = new DataTool(datatoolPath, overwatchPath);
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            var engKeys = datatool.KeysFromLang(Log, "enUS");
            var spKeys = datatool.KeysFromLang(Log, "esES");
            var cKeys = datatool.KeysFromLang(Log, "zhCN");

            List<KeyLink> links = new List<KeyLink>();
            foreach (var element in ElementList.Elements)
                links.Add(GetKeyLink(element.WorkshopName, 5, engKeys, spKeys, cKeys));
            
            Console.Write("Save key links to file: ");
            string saveAt = Console.ReadLine();

            var serializer = new XmlSerializer(typeof(KeyLinkList));
            using (var fileStream = File.Create(saveAt))
            using (StreamWriter writer = new StreamWriter(fileStream))
                serializer.Serialize(writer, new KeyLinkList(links.ToArray()));
        }

        static KeyLink GetKeyLink(string name, int surroundRange, StringKeyGroup engKeys, params StringKeyGroup[] compareTo)
        {
            var pairs = engKeys.AllIndexesFromValue(name);
            int chosen = 0;
            if (pairs.Length > 1)
            {
                KeyLinkTable table = new KeyLinkTable(5, engKeys, compareTo);
                List<KeyLinkRow> rows = new List<KeyLinkRow>();

                for (int i = 0; i < pairs.Length; i++)
                {
                    KeyLinkRow row = new KeyLinkRow(table, pairs[i], i);
                    rows.Add(row);
                }

                foreach (var row in rows)
                    row.Write();

                while (!int.TryParse(Console.ReadLine(), out chosen) || chosen >= pairs.Length);
            }

            return new KeyLink(name, engKeys.FromIndex(pairs[chosen]).Key);
        }

        class KeyLinkTable
        {
            public StringKeyGroup EngKeys { get; }
            public StringKeyGroup[] AltKeys { get; }
            public int Range { get; }

            public KeyLinkTable(int range, StringKeyGroup engKeys, params StringKeyGroup[] altKeys)
            {
                EngKeys = engKeys;
                AltKeys = altKeys;
                Range = range;
            }
        }

        class KeyLinkRow
        {
            private readonly KeyLinkTable _table;
            private readonly int _index;
            private readonly int _optionIndex;
            private readonly int _start;
            private readonly int _end;

            public KeyLinkRow(KeyLinkTable table, int index, int optionIndex)
            {
                _table = table;
                _index = index;
                _optionIndex = optionIndex;
                _start = Math.Max(0, index - table.Range);
                _end = Math.Min(table.EngKeys.List.Count, index + table.Range + 1);
            }

            public void Write()
            {
                int rowLength = 0;
                for (int i = _start; i < _end; i++)
                {
                    StringBuilder lineBuilder = new StringBuilder();

                    if (i == _index)
                        lineBuilder.Append(_optionIndex + ": ");
                    else
                        lineBuilder.Append(new string(' ', _optionIndex.ToString().Length) + "  ");

                    for (int c = 0; c < _table.AltKeys.Length + 1; c++)
                    {
                        int columnLength = ColumnLength(c);
                        var columnKeys = KeysFromColumn(c);
                        string value = columnKeys.FromKey(_table.EngKeys.FromIndex(i).Key).Value;
                        if (value.Length > 20) value = value.Substring(0, 20);
                        
                        lineBuilder.Append(value);
                        lineBuilder.Append(new string(' ', columnLength - value.Length));

                        if (c < _table.AltKeys.Length)
                            lineBuilder.Append(" | ");
                    }

                    Console.WriteLine(lineBuilder);
                    rowLength = Math.Max(rowLength, lineBuilder.Length);
                }
                Console.WriteLine(new string('-', rowLength));
            }

            public int ColumnLength(int column)
            {
                StringKeyGroup useKeys = KeysFromColumn(column);

                int length = 0;
                for (int i = _start; i < _end; i++)
                    length = Math.Min(20, Math.Max(length, useKeys.FromKey(_table.EngKeys.FromIndex(i).Key).Value.Length));
                
                return length;
            }

            private StringKeyGroup KeysFromColumn(int column)
            {
                if (column == 0) return _table.EngKeys;
                return _table.AltKeys[column - 1];
            }
        }
    }

    public class KeyLinkList
    {
        [XmlElement("method")]
        public KeyLink[] Methods { get; set; }

        public KeyLinkList()
        {
        }
        public KeyLinkList(KeyLink[] methods)
        {
            Methods = methods;
        }
    }
    public class KeyLink
    {
        [XmlAttribute("name")]
        public string MethodName { get; set; }
        [XmlAttribute("key")]
        public string Key { get; set; }

        public KeyLink() {}
        public KeyLink(string methodName, string key)
        {
            MethodName = methodName;
            Key = key;
        }
    }

    public class DataTool
    {
        static readonly string[] Languages = new string[] {
            "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW"
        };

        readonly string datatoolPath;
        readonly string overwatchPath;

        public DataTool(string datatoolPath, string overwatchPath)
        {
            this.datatoolPath = datatoolPath;
            this.overwatchPath = overwatchPath;
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

        public StringKeyGroup KeysFromLang(Log log, string language)
        {
            if (log != null) log.Write(LogLevel.Normal, $"Getting {language} keys...");
            var r = CreateKeysFromDump(DumpStrings(language));
            if (log != null) log.Write(LogLevel.Normal, $"Got {language} keys.");
            return r;
        }

        public string DumpStrings(string language)
        {
            if (language == null) throw new ArgumentNullException(nameof(language));
            if (!Languages.Contains(language)) throw new ArgumentException(language + " is not a valid language.", nameof(language));
            return RunCommand("dump-strings --language=" + language);
        }

        public static StringKeyGroup CreateKeysFromDump(string stringDump)
        {
            StringKeyGroup keys = new StringKeyGroup();

            string[] lines = stringDump.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            for (int i = 0; i < lines.Length; i++)
                ParseLine(keys, lines[i]);

            return keys;
        }

        static void ParseLine(StringKeyGroup keys, string line)
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
    }

    public class StringKeyGroup
    {
        public List<StringKey> List { get; } = new List<StringKey>();

        public void Add(StringKey key)
        {
            List.Add(key);
        }
        public void Add(string key, string value)
        {
            Add(new StringKey(key, value));
        }

        public StringKey FromKey(string key) => List.FirstOrDefault(k => k.Key.ToLower() == key.ToLower());
        public StringKey FromValue(string value) => List.FirstOrDefault(k => k.Value.ToLower() == value.ToLower());
        public StringKey[] AllFromValue(string value) => List.Where(k => k.Value.ToLower() == value.ToLower()).ToArray();
        public int[] AllIndexesFromValue(string value)
        {
            List<int> indexes = new List<int>();
            for (int i = 0; i < List.Count; i++)
                if (List[i].Value.ToLower() == value.ToLower())
                    indexes.Add(i);
            return indexes.ToArray();
        }
        public bool ContainsKey(string key) => List.Any(k => k.Key.ToLower() == key.ToLower());
        public bool ContainsValue(string value) => List.Any(k => k.Value.ToLower() == value.ToLower());
        public StringKey FromIndex(int index) => List[index];
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