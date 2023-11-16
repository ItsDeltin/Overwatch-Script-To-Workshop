
namespace Deltin.Deltinteger;

using Deltin.Deltinteger.Elements;
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

        LoadWith(elementsJson: File.ReadAllText(Path.Combine(Program.ExeFolder, "Elements.json")));
    }

    public static void LoadWith(string elementsJson)
    {
        if (IsLoaded)
        {
            return;
        }
        IsLoaded = true;

        ElementRoot.LoadFromJson(elementsJson);
    }
}