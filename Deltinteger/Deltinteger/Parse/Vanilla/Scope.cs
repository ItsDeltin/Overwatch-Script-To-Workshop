#nullable enable

using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Parse.Variables.VanillaLink;

namespace Deltin.Deltinteger.Parse.Vanilla;

interface IAnalyzedVanillaCollection
{
    void AddToScope(VanillaScope scope);

    void AssignWorkshopVariables(
        LinkableVanillaVariables varAssigner,
        VarCollection varCollection,
        SubroutineCollection subroutineCollection);
}

public class DefaultVariableReport
{
    readonly HashSet<VanillaVariable> defaults = new();

    public void Notify(VanillaVariable defaultVanillaVariable)
    {
        defaults.Add(defaultVanillaVariable);
    }

    public void Assign(VarCollection collection, LinkableVanillaVariables linkVanillaVariables)
    {
        foreach (var (Id, Name, IsGlobal) in defaults)
        {
            var ir = collection.Assign(Name, IsGlobal ? VariableType.Global : VariableType.Player, IsGlobal, false, Id);
            linkVanillaVariables.Add(Name, IsGlobal, ir);
        }
    }

    public IEnumerable<VanillaVariable> GetUsedDefaults() => defaults;
}

/// <summary>
/// A collection of vanilla variables accessable via OSTW or Vanilla code.
/// </summary>
public class VanillaScope
{
    readonly static List<string> DefaultVariableNames = new() {
        "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K",
        "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V",
        "W", "X", "Y", "Z"
    };

    readonly DefaultVariableReport report;
    readonly List<VanillaVariable> scopedVariables = new();
    readonly List<VanillaSubroutine> scopedSubroutines = new();

    public VanillaScope(DefaultVariableReport report)
    {
        this.report = report;
    }

    /// <summary>Adds a vanilla variable to the scope.</summary>
    public void AddScopedVariable(VanillaVariable variable) => scopedVariables.Add(variable);

    /// <summary>Gets a by its name and collection type.</summary>
    public VanillaVariable? GetScopedVariable(string name, bool isGlobal)
    {
        var result = scopedVariables.Cast<VanillaVariable?>().FirstOrDefault(var => var is not null && var.Value.Name == name && var.Value.IsGlobal == isGlobal);

        // Find default variable.
        if (result is null)
        {
            var defaultIndex = DefaultVariableNames.IndexOf(name);
            // Ensure that the ID was not overwritten.
            if (defaultIndex != -1 && scopedVariables.All(v => v.IsGlobal != isGlobal || v.Id != defaultIndex))
            {
                result = new VanillaVariable(defaultIndex, name, isGlobal);
                report.Notify(result.Value);
            }
        }

        return result;
    }

    /// <summary>Gets a variable with a matching name of any type.</summary>
    public VanillaVariable? GetScopedVariableOfAnyType(string name) =>
        scopedVariables.AsEnumerable().Reverse().Cast<VanillaVariable?>().FirstOrDefault(
            var => var is not null && var.Value.Name == name);

    /// <summary>Get all variables of a certain type.</summary>
    public IEnumerable<VanillaVariable> GetVariables(bool isGlobal) => scopedVariables.Where(var => var.IsGlobal == isGlobal)
        .Concat(report.GetUsedDefaults().Where(d => d.IsGlobal == isGlobal));

    /// <summary>Adds a subroutine to the scope.</summary>
    public void AddSubroutine(VanillaSubroutine subroutine) => scopedSubroutines.Add(subroutine);

    /// <summary>Gets a subroutine with a matching name.</summary>
    public VanillaSubroutine? GetSubroutine(string name) =>
        scopedSubroutines.AsEnumerable().Reverse().Cast<VanillaSubroutine?>().FirstOrDefault(
            subroutine => subroutine is not null && subroutine.Value.Name == name);

    /// <summary>Gets all subroutines.</summary>
    public IEnumerable<VanillaSubroutine> GetSubroutines() => scopedSubroutines;
}