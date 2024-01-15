#nullable enable

using System;
using System.Linq;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Lobby2.Expand;
using Deltin.Deltinteger.Parse.Vanilla.Ide;

namespace Deltin.Deltinteger.Parse.Vanilla.Settings;

class AnalyzeSettings
{
    public static void Analyze(ScriptFile script, VanillaSettingsGroupSyntax settingsGroup)
    {
        AnalyzeGroup(new(script, LobbySettings.Instance?.Root ?? Array.Empty<EObject>()), settingsGroup);
    }

    static void AnalyzeGroup(SettingsAnalysisContext context, VanillaSettingsGroupSyntax settingsGroup)
    {
        // Add completion.
        context.Script.AddCompletionRange(VanillaCompletion.CreateLobbySettingCompletion(
            settingsGroup.Range,
            context.CurrentObjectChildren));

        // Analyze children.
        foreach (var setting in settingsGroup.Settings)
        {
            switch (setting.Value)
            {
                case VanillaSettingsGroupSyntax subgroup:
                    AnalyzeGroup(context.NewWithChild(setting.Name.Text), subgroup);
                    break;
            }
        }
    }

    record struct SettingsAnalysisContext(ScriptFile Script, EObject[] CurrentObjectChildren)
    {
        public readonly SettingsAnalysisContext NewWithChild(string name)
        {
            var copy = this;
            copy.CurrentObjectChildren = CurrentObjectChildren.FirstOrDefault(child => child.Name == name)
                ?.Children ?? Array.Empty<EObject>();
            return copy;
        }
    }
}