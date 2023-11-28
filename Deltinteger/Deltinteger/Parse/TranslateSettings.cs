using System;
using System.IO;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Settings;
using Deltin.Deltinteger.LanguageServer.Settings;

namespace Deltin.Deltinteger.Parse
{
    public class TranslateSettings
    {
        public Diagnostics Diagnostics { get; }
        public ScriptFile Root { get; }
        public IFileGetter FileGetter { get; } = new LsFileGetter(null, new DefaultSettingsResolver());
        public Func<VarCollection, Rule[]> AdditionalRules { get; set; }

        public SourcedSettings<DsTomlSettings> SourcedSettings { get; set; }

        public OutputLanguage OutputLanguage { get; set; } = OutputLanguage.enUS;

        public TranslateSettings(Diagnostics diagnostics, ScriptFile root)
        {
            Diagnostics = diagnostics;
            Root = root;
        }

        public TranslateSettings(Diagnostics diagnostics, ScriptFile root, IFileGetter fileGetter) : this(diagnostics, root)
        {
            FileGetter = fileGetter;
        }

        public TranslateSettings(Diagnostics diagnostics, Uri root, string content) : this(diagnostics, new ScriptFile(diagnostics, root, content)) { }

        public TranslateSettings(Diagnostics diagnostics, string file)
        {
            Diagnostics = diagnostics;
            Uri uri = new Uri(file);
            string content = File.ReadAllText(file);
            Root = new ScriptFile(diagnostics, uri, content);
        }

        public TranslateSettings(string file) : this(new Diagnostics(), file) { }
    }
}