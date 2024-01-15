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
        // Collect the names of the settings that the user already wrote down.
        // They will not be included in the completion list.
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
        bool parentWasValid = context.CurrentObjectChildren is not null;
        context = context.NewWithChild(setting.Name.Text);

        // Is the setting unknown?
        if (parentWasValid && context.CurrentObject is null)
            context.Warn(setting.Name, $"Unknown lobby setting named '{setting.Name.Text}'");

        bool isMatch = false;

        // Analyze the value.
        switch (setting.Value)
        {
            // Analyze the subsettings.
            case VanillaSettingsGroupSyntax subgroup:
                AnalyzeGroup(context, subgroup);
                isMatch = context.CurrentObject?.Type == EObjectType.Group;
                break;

            // This is probably an option value, or maybe on/off or enabled/disabled.
            case SymbolSettingSyntax symbolSetting:
                switch (context.CurrentObject?.Type)
                {
                    // Check for invalid option
                    case EObjectType.Option:
                        isMatch = context.CurrentObject.Options.Contains(symbolSetting.Symbol.Text);
                        break;
                }
                break;

            case NumberSettingSyntax number:
                isMatch = context.CurrentObject?.Type switch
                {
                    EObjectType.Int or EObjectType.Range => true,
                    _ => false
                };
                break;

            // No value, setting type must be a switch.
            case null:
                isMatch = context.CurrentObject?.Type == EObjectType.Switch;
                break;
        }

        // Add warning if value type is incorrect.
        if (!isMatch && setting.Value is not null && context.CurrentObject is not null)
        {
            switch (context.CurrentObject.Type)
            {
                case EObjectType.Option:
                    context.Warn(
                        setting.Value.ErrorRange,
                        $"Expected {string.Join(", ", context.CurrentObject.Options.Select(option => $"'{option}'"))}");
                    break;

                case EObjectType.Switch:
                    context.Warn(
                        setting.Value.ErrorRange,
                        "Switch settings should not be followed by a value"
                    );
                    break;
            }
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

        public readonly void Warn(DocRange range, string message) => Script.Diagnostics.Warning(message, range);
    }
}