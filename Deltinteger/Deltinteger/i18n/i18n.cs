using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Deltin.Deltinteger.Elements;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.I18n
{
    public class LanguageInfo
    {
        public static OutputLanguage? CurrentLanguage { get; private set; } = null;
        private static I18nLanguage Language;
        private static object LanguageLock = new object();

        public static string Translate(OutputLanguage language, string methodName)
        {
            // if (language == OutputLanguage.enUS) return methodName;

            lock (LanguageLock)
            {
                LoadLanguage(language);
                string translation = Language.Translations.FirstOrDefault(m => m.ID.ToLower() == methodName.ToLower())?.Translation;
                if (translation != null) return translation;
                throw new Exception($"Could not find '{methodName}' in the language file.");
            }
        }

        public static bool IsKeyword(string keyword)
        {
            lock (LanguageLock)
            {
                // if (CurrentLanguage == OutputLanguage.enUS) return true;
                return Language.Translations.Any(m => m.ID == keyword);
            }
        }

        public static void LoadLanguage(OutputLanguage language)
        {
            lock (LanguageLock)
            {
                if (CurrentLanguage != language /*&& language != OutputLanguage.enUS*/)
                {
                    string languageFile = Path.Combine(Program.ExeFolder, "Languages", "i18n-" + language.ToString() + ".json");
                    Language = JsonConvert.DeserializeObject<I18nLanguage>(File.ReadAllText(languageFile));
                }
                CurrentLanguage = language;
            }
        }

        public static void I18nWarningMessage(WorkshopBuilder builder, OutputLanguage outputLanguage)
        {
            if (outputLanguage == OutputLanguage.enUS) return;
            builder.AppendLine($"// Outputting to the language {outputLanguage.ToString()}.");
            builder.AppendLine($"// Not all languages are tested. If a value is not outputting correctly, you can change");
            builder.AppendLine($"// the keyword info in the Languages/i18n-{outputLanguage.ToString()}.json file.");
            builder.AppendLine();
        }
    }

    public class I18nLanguage
    {
        public I18nLanguage() { }

        [JsonProperty("translations")]
        public IList<I18nTranslation> Translations { get; set; } = new List<I18nTranslation>();
    }

    public class I18nTranslation
    {
        public I18nTranslation() { }
        public I18nTranslation(string id, string translation)
        {
            ID = id;
            Translation = translation;
        }

        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("translation")]
        public string Translation { get; set; }
    }
}