#nullable enable
using System.Collections.Generic;
namespace Deltin.Deltinteger.Parse.Variables.VanillaLink;

/// <summary>
/// Vanilla variables assigned to IndexReferences while compiling.
/// </summary>
public class LinkableVanillaVariables
{
    readonly Dictionary<string, IndexReference> global = new();
    readonly Dictionary<string, IndexReference> player = new();

    public void Add(string name, bool isGlobal, IndexReference value)
    {
        (isGlobal ? global : player).Add(name, value);
    }

    public IndexReference? GetVanillaVariable(string name, bool isGlobal)
    {
        return (isGlobal ? global : player).GetValueOrDefault(name);
    }
}