#nullable enable
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
namespace Deltin.Deltinteger.Parse.Variables.VanillaLink;

/// <summary>
/// Vanilla variables assigned to IndexReferences while compiling.
/// </summary>
public class LinkableVanillaVariables
{
    readonly Dictionary<string, IndexReference> global = new();
    readonly Dictionary<string, IndexReference> player = new();
    readonly Dictionary<string, Subroutine> subroutines = new();

    public void Add(string name, bool isGlobal, IndexReference value)
    {
        (isGlobal ? global : player).Add(name, value);
    }

    public IndexReference? GetVanillaVariable(string name, bool isGlobal)
    {
        return (isGlobal ? global : player).GetValueOrDefault(name);
    }

    public void AddSubroutine(string name, Subroutine value) => subroutines.Add(name, value);

    public Subroutine? GetSubroutine(string name) => subroutines.GetValueOrDefault(name);
}