#nullable enable
using System;
using Deltin.Deltinteger.LanguageServer.Settings;
using Deltin.Deltinteger.Parse.Settings;

namespace Deltin.Deltinteger.LanguageServer.Model;

/// <summary>Gets a DsTomlSettings for a script compilation.</summary>
public interface IDsSettingsProvider
{
    public SourcedSettings<DsTomlSettings> GetProjectSettings(Uri uri);

    void GetInitialFiles();

    public static IDsSettingsProvider New(Func<Uri, SourcedSettings<DsTomlSettings>> getProjectSettingsFunc)
        => new DsSettingsProvider(getProjectSettingsFunc);

    record DsSettingsProvider(Func<Uri, SourcedSettings<DsTomlSettings>> GetProjectSettingsFunc) : IDsSettingsProvider
    {
        public void GetInitialFiles() { }

        public SourcedSettings<DsTomlSettings> GetProjectSettings(Uri uri) => GetProjectSettingsFunc(uri);
    }
}