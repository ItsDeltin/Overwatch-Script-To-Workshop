using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.I18n
{
    public class LanguageInfo
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
            lock (LanguageLock)
            {
                if (CurrentLanguage != language && language != OutputLanguage.enUS)
                {
                    string languageFile = Path.Combine(Program.ExeFolder, "Languages", "i18n-" + language.ToString() + ".xml");
                    XmlSerializer serializer = new XmlSerializer(typeof(I18nLanguage));

                    using (var fileStream = File.OpenRead(languageFile))
                        Language = (I18nLanguage)serializer.Deserialize(fileStream);
                }
                CurrentLanguage = language;
            }
        }

        public static void I18nWarningMessage(WorkshopBuilder builder, OutputLanguage outputLanguage)
        {
            if (outputLanguage == OutputLanguage.enUS) return;
            builder.AppendLine($"// Outputting to the language {outputLanguage.ToString()}.");
            builder.AppendLine($"// Not all languages are tested. If a value is not outputting correctly, you can change");
            builder.AppendLine($"// the keyword info in the Languages/i18n-{outputLanguage.ToString()}.xml file.");
            builder.AppendLine();
        }
    }

    public class I18nLanguage
    {
        public I18nLanguage() {}

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