#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Lobby2.Expand;
using Deltin.Deltinteger.Lobby2.KeyValues;
using Deltin.Deltinteger.Model;
using Deltin.Deltinteger.Parse.Vanilla.Ide;
using Deltin.WorkshopString;

namespace Deltin.Deltinteger.Parse.Vanilla.Settings;

static class AnalyzeSettings
{
    public static GroupSettingValue Analyze(ScriptFile script, VanillaSettingsGroupSyntax settingsGroup)
    {
        return AnalyzeGroup(new(script, null, LobbySettings.Instance?.Root ?? Array.Empty<EObject>()), settingsGroup);
    }

    static GroupSettingValue AnalyzeGroup(SettingsAnalysisContext context, VanillaSettingsGroupSyntax settingsGroup)
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
        var keyValues = new List<SettingKeyValue>();
        foreach (var setting in settingsGroup.Settings)
        {
            keyValues.Add(AnalyzeSetting(context, setting));
        }

        return new(keyValues);
    }

    static SettingKeyValue AnalyzeSetting(SettingsAnalysisContext context, VanillaSettingSyntax setting)
    {
        bool parentWasValid = context.CurrentObjectChildren is not null;
        context = context.NewWithChild(setting.Name.Text);

        // Is the setting unknown?
        if (parentWasValid && context.CurrentObject is null)
            context.Warn(setting.Name, $"Unknown lobby setting named '{setting.Name.Text}'");

        bool isMatch = false;

        ISettingValue? value = null;

        // Analyze the value.
        switch (setting.Value)
        {
            // Analyze the subsettings.
            case VanillaSettingsGroupSyntax subgroup:
                value = AnalyzeGroup(context, subgroup);
                isMatch = context.CurrentObject?.Type == EObjectType.Group;
                break;

            // This is probably an option value, or maybe on/off or enabled/disabled.
            case SymbolSettingSyntax symbolSetting:
                value = new OptionSettingValue(symbolSetting.Symbol.Text);
                switch (context.CurrentObject?.Type)
                {
                    // Check for invalid option
                    case EObjectType.Option:
                        isMatch = context.CurrentObject.Options.Contains(symbolSetting.Symbol.Text);
                        break;
                }
                break;

            // Number value
            case NumberSettingSyntax number:
                value = new NumberSettingValue(double.Parse(number.Value.Text), number.PercentSign);
                switch (context.CurrentObject?.Type)
                {
                    case EObjectType.Int or EObjectType.Range:
                        isMatch = true;

                        // Make sure the percent sign or lack thereof is valid.
                        if (number.PercentSign is null && context.CurrentObject.Type == EObjectType.Range)
                        {
                            context.Warn(number.Value, "Range settings should be followed by %");
                        }
                        else if (number.PercentSign is not null && context.CurrentObject.Type == EObjectType.Int)
                        {
                            context.Warn(number.PercentSign, "Integer settings should not be followed by %");
                        }
                        break;

                    default:
                        isMatch = false;
                        break;
                };
                break;

            // String value
            case StringSettingSyntax str:
                value = new StringSettingValue(WorkshopStringUtility.WorkshopStringFromRawText(str.Value.Text));
                isMatch = context.CurrentObject?.Type == EObjectType.String;
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

        Variant<EObject, string> source = Variant.AElseB(context.CurrentObject, setting.Name.Text);
        return new(source, value);
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