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

/// <summary>
/// A collection of vanilla variables accessable via OSTW or Vanilla code.
/// </summary>
public class VanillaScope
{
    readonly List<VanillaVariable> scopedVariables = new();
    readonly List<VanillaSubroutine> scopedSubroutines = new();

    /// <summary>Adds a vanilla variable to the scope.</summary>
    public void AddScopedVariable(VanillaVariable variable) => scopedVariables.Add(variable);

    /// <summary>Gets a by its name and collection type.</summary>
    public VanillaVariable? GetScopedVariable(string name, bool isGlobal) =>
        scopedVariables.Cast<VanillaVariable?>().FirstOrDefault(var => var is not null && var.Value.Name == name && var.Value.IsGlobal == isGlobal);

    /// <summary>Gets a variable with a matching name of any type.</summary>
    public VanillaVariable? GetScopedVariableOfAnyType(string name) =>
        scopedVariables.AsEnumerable().Reverse().Cast<VanillaVariable?>().FirstOrDefault(
            var => var is not null && var.Value.Name == name);

    /// <summary>Get all variables of a certain type.</summary>
    public IEnumerable<VanillaVariable> GetVariables(bool isGlobal) => scopedVariables.Where(var => var.IsGlobal == isGlobal);

    /// <summary>Adds a subroutine to the scope.</summary>
    public void AddSubroutine(VanillaSubroutine subroutine) => scopedSubroutines.Add(subroutine);

    /// <summary>Gets a subroutine with a matching name.</summary>
    public VanillaSubroutine? GetSubroutine(string name) =>
        scopedSubroutines.AsEnumerable().Reverse().Cast<VanillaSubroutine?>().FirstOrDefault(
            subroutine => subroutine is not null && subroutine.Value.Name == name);

    /// <summary>Gets all subroutines.</summary>
    public IEnumerable<VanillaSubroutine> GetSubroutines() => scopedSubroutines;
}