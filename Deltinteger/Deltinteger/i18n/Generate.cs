using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.I18n
{
    public class GenerateI18n
    {
        static readonly Log Log = new Log("i18n");
        static readonly string syntax = "deltinteger i18n \"datatool file location\" language \"output file\" \"[overwatch file location]\"";

        static readonly string[] ProcLanguages = new string[] {
            "enUS", "deDE", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW"
        };

        public static void Generate(string[] args)
        {
            string datatoolPath = args.ElementAtOrDefault(1);
            string keyLinkFile = args.ElementAtOrDefault(2);
            string outputFile = args.ElementAtOrDefault(3);
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

            // Create the datatool instance.
            var datatool = new Dump.DataTool(datatoolPath, overwatchPath);
            // Get the key links.
            KeyLink[] keyLinks = JsonConvert.DeserializeObject<KeyLinkList>(File.ReadAllText(keyLinkFile)).Links;
            // Get all keywords.
            var keywords = Keyword.GetKeywords();

            foreach (string lang in ProcLanguages)
            {
                // Dump the strings for the language.
                StringKeyGroup strings = new StringKeyGroup();
                strings.DumpStrings(datatool, lang, true, Log);

                I18nLanguage result = new I18nLanguage();

                // Translate
                foreach (var keyword in keywords)
                    result.Translations.Add(new I18nTranslation(
                        keyword.ID,
                        strings.ValueFromKeyAndLang(keyLinks.First(m => m.ID.ToLower() == keyword.ID.ToLower()).Key, lang)
                    ));

                // Get the file
                string file = Path.Combine(outputFile, "i18n-" + lang + ".json");

                if (File.Exists(file))
                    File.Delete(file);

                // Serialize
                using (var fileStream = File.Create(file))
                using (StreamWriter writer = new StreamWriter(fileStream))
                    writer.Write(JsonConvert.SerializeObject(result));

                Log.Write(LogLevel.Normal, "Finished " + lang + ".");
            }
        }

        public static void GenerateKeyLink()
        {
            string datatoolPath = "C:/Users/Deltin/Downloads/toolchain-release/DataTool.exe";
            string overwatchPath = "C:/Program Files (x86)/Overwatch";
            string previous = "C:/Users/Deltin/Documents/GitHub/Overwatch-Script-To-Workshop/Deltinteger/Deltinteger/Languages/key_links.json";
            string saveAt = previous;

            Console.OutputEncoding = System.Text.Encoding.Unicode;

            Dump.DataTool datatool = new Dump.DataTool(datatoolPath, overwatchPath);
            StringKeyGroup strings = new StringKeyGroup();

            strings.DumpStrings(datatool, "enUS", true, Log);
            strings.DumpStrings(datatool, "esES", false, Log);
            strings.DumpStrings(datatool, "esMX", false, Log);
            strings.DumpStrings(datatool, "itIT", false, Log);

            List<KeyLink> links = new List<KeyLink>();

            if (previous != null && File.Exists(previous))
            {
                var last = JsonConvert.DeserializeObject<KeyLinkList>(File.ReadAllText(previous)).Links;
                links.AddRange(last);
            }

            var keywords = Keyword.GetKeywords();

            foreach (var keyword in keywords)
                if (!links.Any(link => link.ID.ToLower() == keyword.ID.ToLower()))
                    GetKeyLink(links, keyword, 5, strings);

            while (true)
            {
                Console.WriteLine("Write name of link to redo.");
                string input = Console.ReadLine();
                if (input == "") break;

                var existing = keywords.FirstOrDefault(k => k.ID == input);

                if (existing == null) Console.WriteLine($"No keywords by that name exists.");
                else
                {
                    var link = links.FirstOrDefault(l => l.ID.ToLower() == input.ToLower());
                    links.Remove(link);
                    GetKeyLink(links, existing, 5, strings);
                }
            }

            using (var fileStream = File.Create(saveAt))
            using (StreamWriter writer = new StreamWriter(fileStream))
                writer.Write(JsonConvert.SerializeObject(new KeyLinkList(links.ToArray())));
        }

        static void GetKeyLink(List<KeyLink> links, Keyword keyword, int surroundRange, StringKeyGroup strings)
        {
            string name = keyword.Name;

            var pairs = strings.KeysFromEnglishName(name);
            int chosen = 0;
            if (pairs.Length > 1)
            {
                // Make sure there are no duplicate strings.
                bool allEqual = true;
                for (int i = 1; i < pairs.Length && allEqual; i++)
                    for (int c = 0; c < pairs[i].Translations.Count && allEqual; c++)
                        foreach (var pair in pairs)
                            if (pair.Translations[c].Text != pairs[0].Translations[c].Text)
                                allEqual = false;
                if (!allEqual)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(keyword.ID);
                    Console.ForegroundColor = ConsoleColor.White;

                    List<KeyLinkRow> rows = new List<KeyLinkRow>();

                    for (int i = 0; i < pairs.Length; i++)
                    {
                        KeyLinkRow row = new KeyLinkRow(strings, pairs[i], i, 5);
                        rows.Add(row);
                    }

                    foreach (var row in rows)
                        row.Write();

                    while (!int.TryParse(Console.ReadLine(), out chosen) || chosen >= pairs.Length) ;
                }
            }
            else if (pairs.Length == 0)
            {
                Console.WriteLine($"Error: no pairs found for '{name}'.");
                Console.ReadLine();
                return;
            }
            links.Add(new KeyLink(keyword.ID, pairs[chosen].Key));
        }

        class KeyLinkRow
        {
            private readonly StringKeyGroup _strings;
            private readonly StringKey _pair;
            private readonly int _index;
            private readonly int _optionIndex;
            private readonly int _start;
            private readonly int _end;

            public KeyLinkRow(StringKeyGroup strings, StringKey pair, int optionIndex, int range)
            {
                _strings = strings;
                _pair = pair;
                _index = strings.IndexOf(pair);
                _optionIndex = optionIndex;
                _start = Math.Max(0, _index - range);
                _end = Math.Min(strings.List.Count, _index + range + 1);
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

                    for (int c = 0; c < _strings.Languages.Count; c++)
                    {
                        int columnLength = ColumnLength(c);

                        string value = _strings.FromIndex(i).Translations[c].Text;
                        if (value.Length > 20) value = value.Substring(0, 20);

                        lineBuilder.Append(value);
                        lineBuilder.Append(new string(' ', columnLength - value.Length));

                        if (c < _strings.Languages.Count - 1)
                            lineBuilder.Append(" | ");
                    }

                    if (i == _index) Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine(lineBuilder);
                    Console.ForegroundColor = ConsoleColor.White;
                    rowLength = Math.Max(rowLength, lineBuilder.Length);
                }
                Console.WriteLine(new string('-', rowLength));
            }

            public int ColumnLength(int column)
            {
                int length = 0;
                for (int i = _start; i < _end; i++)
                    length = Math.Min(20, Math.Max(length, _strings.FromIndex(i).Translations[column].Text.Length));

                return length;
            }
        }
    }

    public class KeyLinkList
    {
        [JsonProperty("links")]
        public KeyLink[] Links { get; set; }

        public KeyLinkList()
        {
        }
        public KeyLinkList(KeyLink[] links)
        {
            Links = links;
        }
    }
    public class KeyLink
    {
        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("key")]
        public string Key { get; set; }

        public KeyLink() { }
        public KeyLink(string id, string key)
        {
            ID = id;
            Key = key;
        }
    }
}