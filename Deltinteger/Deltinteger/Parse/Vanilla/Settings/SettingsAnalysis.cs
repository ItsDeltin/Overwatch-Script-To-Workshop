#nullable enable

using System;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Lobby2.Expand;
using Deltin.Deltinteger.Parse.Vanilla.Ide;

namespace Deltin.Deltinteger.Parse.Vanilla.Settings;

class AnalyzeSettings
{
    public static void Analyze(ScriptFile script, VanillaSettingsGroupSyntax settingsGroup)
    {
        AnalyzeGroup(new(script, null, LobbySettings.Instance?.Root ?? Array.Empty<EObject>()), settingsGroup);
    }

    static void AnalyzeGroup(SettingsAnalysisContext context, VanillaSettingsGroupSyntax settingsGroup)
    {
        var alreadyIncluded = settingsGroup.Settings.Select(setting => setting.Name.Text);

        // Add completion.
        context.Script.AddCompletionRange(VanillaCompletion.CreateLobbySettingCompletion(
            settingsGroup.Range,
            context.CurrentObjectChildren,
            alreadyIncluded));

        // Analyze children.
        foreach (var setting in settingsGroup.Settings)
        {
            AnalyzeSetting(context, setting);
        }
    }

    static void AnalyzeSetting(SettingsAnalysisContext context, VanillaSettingSyntax setting)
    {
        context = context.NewWithChild(setting.Name.Text);

        switch (setting.Value)
        {
            // Analyze the subsettings.
            case VanillaSettingsGroupSyntax subgroup:
                AnalyzeGroup(context, subgroup);
                break;
        }

        // Add completion for key-value pairs.
        if (context.CurrentObject is not null &&
            setting.Colon is not null &&
            setting.TokenAfterColon is not null)
        {
            var hintRange = setting.Colon.Range.End + setting.TokenAfterColon.Range.End;

            context.AddCompletion(context.CurrentObject.Type switch
            {
                EObjectType.OnOff => VanillaCompletion.CreateKeywords(hintRange, "On", "Off"),
                EObjectType.EnabledDisabled => VanillaCompletion.CreateKeywords(hintRange, "Enabled", "Disabled"),
                EObjectType.Option => VanillaCompletion.CreateKeywords(hintRange, context.CurrentObject.Options),
                _ => VanillaCompletion.Clear(hintRange)
            });
        }
    }

    record struct SettingsAnalysisContext(ScriptFile Script, EObject? CurrentObject, EObject[] CurrentObjectChildren)
    {
        public readonly SettingsAnalysisContext NewWithChild(string name)
        {
            var copy = this;
            copy.CurrentObject = CurrentObjectChildren.FirstOrDefault(child => child.Name == name);
            copy.CurrentObjectChildren = copy.CurrentObject?.Children ?? Array.Empty<EObject>();
            return copy;
        }

        public readonly void AddCompletion(ICompletionRange completion) => Script.AddCompletionRange(completion);

        public readonly Token NextToken(Token previousToken) => Script.NextToken(previousToken);
    }
}