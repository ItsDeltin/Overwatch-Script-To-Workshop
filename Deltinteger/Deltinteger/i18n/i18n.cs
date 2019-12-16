using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.I18n
{
    public class I18n
    {
        public static OutputLanguage CurrentLanguage { get; private set; } = OutputLanguage.enUS;
        private static I18nLanguage Language;
        private static object LanguageLock = new object();

        public static string Translate(OutputLanguage language, string methodName)
        {
            if (language == OutputLanguage.enUS) return methodName;

            lock (LanguageLock)
            {
                if (CurrentLanguage != language)
                    throw new Exception($"The '{language.ToString()}' language is not loaded.");
                
                return Language.Methods.FirstOrDefault(m => m.EnglishName == methodName)?.Translation
                    ?? throw new Exception($"Could not find '{methodName}' in the language file.");
            }
        }

        public static void LoadLanguage(OutputLanguage language)
        {
            if (language != OutputLanguage.enUS)
            lock (LanguageLock)
            if (CurrentLanguage != language)
            {
                string languageFile = Path.Combine(Program.ExeFolder, "Languages", "i18n-" + language.ToString() + ".xml");
                XmlSerializer serializer = new XmlSerializer(typeof(I18nLanguage));

                using (var fileStream = File.OpenRead(languageFile))
                    Language = (I18nLanguage)serializer.Deserialize(fileStream);

                CurrentLanguage = language;
            }
        }
    }

    public class I18nLanguage
    {
        public I18nLanguage() {}
        public I18nLanguage(Log log, StringKeyGroup engKeys, StringKeyGroup altKeys)
        {
            foreach (var element in ElementList.Elements)
            {
                string engName = element.WorkshopName;
                var engPair = engKeys.FromValue(engName);

                if (engPair == null) throw new Exception($"Could not find key pair for value '{engName}'.");

                var altPair = altKeys.FromKey(engPair.Key);
                if (engName.ToLower() != altPair.Value.ToLower())
                    Methods.Add(new I18nMethod(engName, altPair.Value));
            }
        }

        [XmlArrayItem("method")]
        public List<I18nMethod> Methods { get; } = new List<I18nMethod>();
    }

    public class I18nMethod
    {
        public I18nMethod() {}
        public I18nMethod(string englishName, string translation)
        {
            EnglishName = englishName;
            Translation = translation;
        }

        [XmlAttribute("name")]
        public string EnglishName { get; set; }
        [XmlAttribute("alt")]
        public string Translation { get; set; }
    }
}