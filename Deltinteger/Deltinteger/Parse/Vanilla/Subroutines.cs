#nullable enable

using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Variables.VanillaLink;

namespace Deltin.Deltinteger.Parse.Vanilla;

/// <summary>A vanilla subroutine declared in a `subroutines {}` expression.</summary>
public readonly record struct VanillaSubroutine(int Id, string Name);

readonly struct VanillaSubroutineAnalysis : IAnalyzedVanillaCollection
{
    readonly List<VanillaSubroutine> vanillaSubroutines;

    VanillaSubroutineAnalysis(List<VanillaSubroutine> vanillaSubroutines) => this.vanillaSubroutines = vanillaSubroutines;

    public void AddToScope(VanillaScope scope)
    {
        foreach (var subroutine in vanillaSubroutines)
            scope.AddSubroutine(subroutine);
    }

    public void AssignWorkshopVariables(LinkableVanillaVariables varAssigner, VarCollection varCollection, SubroutineCollection subroutineCollection)
    {
        foreach (var subroutine in vanillaSubroutines)
        {
            var subroutineElement = varAssigner.GetSubroutine(subroutine.Name)
                ?? subroutineCollection.NewSubroutine(subroutine.Name, subroutine.Id);

            varAssigner.AddSubroutine(subroutine.Name, subroutineElement);
        }
    }

    public static VanillaSubroutineAnalysis Analyze(ScriptFile script, VanillaVariableCollection syntax)
    {
        var subroutines = new List<VanillaSubroutine>();

        foreach (var item in syntax.Items)
        {
            if (item.Group.HasValue)
            {
                script.Diagnostics.Error("Subroutines cannot be grouped", item.Group.Value.GroupToken);
            }
            else if (item.Name.HasValue)
            {
                var (inputId, name) = item.Name.Value;
                var range = item.Name.Value!.Range;

                if (!int.TryParse(inputId.Text, out int id))
                    script.Diagnostics.Warning("Variable ids must be integers", range);

                if (subroutines.Any(subroutine => subroutine.Name == name!.Text))
                    script.Diagnostics.Warning($"Duplicate workshop subroutine name '{name!.Text}'", range);

                else if (name is not null)
                    subroutines.Add(new(id, name.Text));
            }
        }

        return new(subroutines);
    }
}