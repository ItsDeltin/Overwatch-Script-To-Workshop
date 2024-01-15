
namespace Deltin.Deltinteger;

using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby2.Expand;
using System.IO;

static class LoadData
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
            settingsJson: FromApplicationFolder("LobbySettings.json")
        );
    }

    public static void LoadWith(string elementsJson, string settingsJson)
    {
        if (IsLoaded)
        {
            return;
        }
        IsLoaded = true;

        ElementRoot.LoadFromJson(elementsJson);
        LobbySettings.LoadFromJson(settingsJson);
    }

    static string FromApplicationFolder(string name)
    {
        return File.ReadAllText(Path.Combine(Program.ExeFolder, name));
    }
}