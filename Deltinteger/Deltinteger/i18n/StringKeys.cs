using System;
using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.I18n
{
    public class StringKeyGroup
    {
        public List<StringKey> List { get; } = new List<StringKey>();
        public List<string> Languages { get; } = new List<string>();

        public void Add(string key, string lang, string text, bool addNewKeys)
        {
            foreach (var sk in List)
                if (sk.Key == key)
                {
                    if (!Languages.Contains(lang))
                        Languages.Add(lang);

                    sk.Translations.Add(new StringKeyLang(lang, text));
                    return;
                }

            if (!addNewKeys) return;

            var newKey = new StringKey(key);
            newKey.Translations.Add(new StringKeyLang(lang, text));
            List.Add(newKey);
        }

        public string ValueFromKeyAndLang(string key, string lang) => List.First(strKey => strKey.Key == key).Translations.First(tr => tr.Lang == lang).Text;
        public StringKey[] KeysFromEnglishName(string name) => List
            .Where(strKey => strKey.Translations.Any(tr => tr.Lang == "enUS" && tr.Text.ToLower() == name.ToLower()))
            .ToArray();
        public int IndexOf(StringKey stringKey) => List.IndexOf(stringKey);
        public StringKey FromIndex(int index) => List[index];
    }
    
    public class StringKey
    {
        public string Key { get; }
        public List<StringKeyLang> Translations { get; } = new List<StringKeyLang>();

        public StringKey(string key)
        {
            Key = key;
        }
    }

    public class StringKeyLang
    {
        public string Lang { get; }
        public string Text { get; }

        public StringKeyLang(string lang, string text)
        {
            Lang = lang;
            Text = text;
        }
    }
}