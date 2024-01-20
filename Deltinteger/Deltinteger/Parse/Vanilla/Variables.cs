#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Vanilla.Ide;
using Deltin.Deltinteger.Parse.Variables.VanillaLink;

namespace Deltin.Deltinteger.Parse.Vanilla;

/// <summary>
/// A declared vanilla variable.
/// </summary>
public readonly record struct VanillaVariable(int Id, string Name, bool IsGlobal);

/// <summary>
/// Analyzes a vanilla 'variables' declaration in the script.
/// </summary>
class VanillaVariableAnalysis : IAnalyzedVanillaCollection
{
    readonly IReadOnlyList<VanillaVariable> vanillaVariables;

    VanillaVariableAnalysis(List<VanillaVariable> vanillaVariables) => this.vanillaVariables = vanillaVariables;

    /// <summary>Adds vanilla variables to the scope for OSTW access and vanilla completion.</summary>
    public void AddToScope(VanillaScope scope)
    {
        foreach (var vanillaVariable in vanillaVariables)
            scope.AddScopedVariable(vanillaVariable);
    }

    /// <summary>Assigns the vanilla variables to the workshop.</summary>
    public void AssignWorkshopVariables(LinkableVanillaVariables varAssigner, VarCollection varCollection, SubroutineCollection subroutineCollection)
    {
        foreach (var vanillaVariable in vanillaVariables)
        {
            var indexReference = varAssigner.GetVariable(vanillaVariable.Name, vanillaVariable.IsGlobal)
                // No existing variable, create it.
                ?? varCollection.Assign(vanillaVariable.Name, VariableType.Dynamic, vanillaVariable.IsGlobal, false, vanillaVariable.Id);

            varAssigner.Add(vanillaVariable.Name, vanillaVariable.IsGlobal, indexReference);
        }
    }

    public static VanillaVariableAnalysis Analyze(ScriptFile script, VanillaVariableCollection syntax)
    {
        // Add completion
        script.AddCompletionRange(VanillaCompletion.CreateKeywords(syntax.Range, "global", "player"));

        var vanillaVariables = new List<VanillaVariable>();
        var currentGroup = CurrentGroup.None;
        var didAddNoGroupError = false;
        var didGetGroup = false;

        foreach (var item in syntax.Items)
        {
            // Name
            if (item.Name.HasValue && item.Name.Value.Name)
            {
                string name = item.Name.Value.Name!.Text;

                // Make sure the variable is being added to a group.
                if (currentGroup == CurrentGroup.None)
                {
                    // An error already happened, try to not spam the user since the issue is elsewhere.
                    if (!didAddNoGroupError && !didGetGroup)
                    {
                        script.Diagnostics.Warning("Added variable without assigning 'global' or 'player' group", item.Name.Value.Range);
                        didAddNoGroupError = true;
                    }
                }
                // Check for duplicates.
                else if (vanillaVariables.Any(otherItem => name == otherItem.Name))
                    script.Diagnostics.Warning($"Duplicate workshop variable name '{item.Name}'", item.Name.Value.Range);
                else
                {
                    int id = -1;
                    if (!int.TryParse(item.Name.Value.Id.Text, out id))
                        script.Diagnostics.Warning("Variable ids must be integers", item.Name.Value.Range);

                    bool isGlobal = currentGroup == CurrentGroup.Global;
                    vanillaVariables.Add(new(id, name, isGlobal));
                }
            }
            // Group
            else if (item.Group.HasValue)
            {
                didGetGroup = true; // prevent the 'no group' error from being added.
                string groupName = item.Group.Value.GroupToken.Text;

                // Global
                if (VanillaInfo.GlobalVariableGroup.Match(groupName))
                    currentGroup = CurrentGroup.Global;
                // Player
                else if (VanillaInfo.PlayerVariableGroup.Match(groupName))
                    currentGroup = CurrentGroup.Player;
                // Unknown
                else
                {
                    currentGroup = CurrentGroup.None;
                    script.Diagnostics.Warning("Unknown variable group name, expected 'global' or 'player'", item.Group.Value.GroupToken);
                }
            }
        }

        return new VanillaVariableAnalysis(vanillaVariables);
    }

    enum CurrentGroup
    {
        None,
        Global,
        Player
    }
}