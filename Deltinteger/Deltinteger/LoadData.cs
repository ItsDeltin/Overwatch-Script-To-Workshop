
namespace Deltin.Deltinteger;

using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby;
using Deltin.Deltinteger.Lobby2.Expand;
using System;
using System.IO;

public static class LoadData
{
    public static bool IsLoaded { get; private set; } = false;

    public static void LoadFromFileSystem()
    {
        if (IsLoaded)
        {
            return;
        }

        LoadWith(
            elementsJson: FromApplicationFolder("Elements.json"),
            settingsJson: FromApplicationFolder("LobbySettings.json"),
            mapsJson: FromApplicationFolder("Maps.json")
        );
    }

    public static void LoadWith(string elementsJson, string settingsJson, string mapsJson)
    {
        if (IsLoaded)
        {
            return;
        }
        IsLoaded = true;

        LobbyMap.LoadFromJson(mapsJson);
        ElementRoot.LoadFromJson(elementsJson);
        LobbySettings.LoadFromJson(settingsJson);
    }

    static string FromApplicationFolder(string name)
    {
        try
        {
            return File.ReadAllText(Path.Combine(Program.ExeFolder, name));
        }
        catch (Exception ex)
        {
            ErrorReport.Add($"Failed to read {name}: {ex}");
            return string.Empty;
        }
    }
}