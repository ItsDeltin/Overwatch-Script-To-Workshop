using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Settings;
using Deltin.Deltinteger.LanguageServer.Settings;

#nullable enable

namespace Deltin.Deltinteger.Parse;

public class TranslateSettings
{
    public Diagnostics Diagnostics { get; }
    public IFileGetter FileGetter { get; }
    public Func<VarCollection, Rule[]>? AdditionalRules { get; set; }
    public SourcedSettings<DsTomlSettings> SourcedSettings { get; set; }
    public OutputLanguage OutputLanguage { get; set; } = OutputLanguage.enUS;
    public Uri EntryPoint { get; set; }

    public TranslateSettings(Uri entryPoint, Diagnostics diagnostics, IFileGetter fileGetter)
    {
        EntryPoint = entryPoint;
        Diagnostics = diagnostics;
        FileGetter = fileGetter;
    }

    public TranslateSettings(Uri entryPoint) : this(entryPoint, new Diagnostics(), new LsFileGetter(null, new DefaultSettingsResolver()))
    {
    }

    public TranslateSettings(Diagnostics diagnostics, string content)
    {
        var uri = new Uri("inmemory://temp.ostw");
        EntryPoint = uri;
        Diagnostics = diagnostics;
        FileGetter = new MultiSourceFileGetter([
            new StaticFileGetter(uri, content),
            new LsFileGetter(null, new DefaultSettingsResolver())
        ]);
    }
}