using System;
using System.IO;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Settings;

namespace Deltin.Deltinteger.Parse
{
    public class TranslateSettings
    {
        public Diagnostics Diagnostics { get; }
        public ScriptFile Root { get; }
        public FileGetter FileGetter { get; } = new FileGetter(null, new LanguageServer.Settings.DefaultSettingsResolver());
        public Func<VarCollection, Rule[]> AdditionalRules { get; set; }

        public ProjectSettings Settings { get; set; }

        public bool OptimizeOutput { get; set; } = true;
        public OutputLanguage OutputLanguage { get; set; } = OutputLanguage.enUS;

        public TranslateSettings(Diagnostics diagnostics, ScriptFile root)
        {
            Diagnostics = diagnostics;
            Root = root;
        }

        public TranslateSettings(Diagnostics diagnostics, ScriptFile root, FileGetter fileGetter) : this(diagnostics, root)
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