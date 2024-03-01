#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Lobby2.Expand;
using Deltin.Deltinteger.Lobby2.KeyValues;
using Deltin.Deltinteger.Model;
using Deltin.Deltinteger.Parse;
using Deltin.WorkshopString;
using Newtonsoft.Json.Linq;

namespace Deltin.Deltinteger.Lobby2.Legacy;

/// <summary>Validates and converts the old "customGameSettings.json" imported files.</summary>
static class ParseLegacySettingsJson
{
    /// <summary>Converts a json string to a GroupSettingValue. Errors are reported to the provided script
    /// and range.</summary>
    public static GroupSettingValue ParseJson(string json, ScriptFile script, DocRange reportRange)
    {
        var (value, diagnostics) = Result.Try(() => JObject.Parse(json)).Match(
            CheckTop,
            // JSON parse error
            err => (new(), new[] {
                new LegacyJsonDiagnostic(err, LegacyJsonDiagnosticType.Error)
            })
        );

        // Report collected errors.
        foreach (var diagnostic in diagnostics)
        {
            switch (diagnostic.DiagnosticType)
            {
                case LegacyJsonDiagnosticType.Warning:
                    script.Diagnostics.Warning(diagnostic.Message, reportRange);
                    break;

                case LegacyJsonDiagnosticType.Error:
                default:
                    script.Diagnostics.Error(diagnostic.Message, reportRange);
                    break;
            }
        }

        return value;
    }

    static (GroupSettingValue, IReadOnlyList<LegacyJsonDiagnostic>) CheckTop(JObject obj)
    {
        var top = new GroupSettingValue();
        var diagnostics = new List<LegacyJsonDiagnostic>();
        CheckObject(obj, new(Enumerable.Empty<string>(), top, diagnostics));
        return (top, diagnostics);
    }

    static void CheckObject(JObject obj, TravelParams travelParams)
    {
        // Analyze each property.
        foreach (var prop in obj.Properties())
            AnalyzeProperty(prop.Value, travelParams.Step(prop.Name));
    }

    static void AnalyzeProperty(JToken propValue, TravelParams travelParams)
    {
        // Recursively check objects.
        if (propValue.Type == JTokenType.Object)
        {
            // This ensures modes with empty objects are added to the output.
            var (_, newPath, _) = MatchLegacyPath(travelParams.Path);
            KeyValueFromPath(travelParams.TopGroup!, newPath.ToArray());

            CheckObject((JObject)propValue, travelParams);
        }
        // Array of switches
        else if (propValue.Type == JTokenType.Array)
        {
            foreach (var arrayItem in ((JArray)propValue).Values())
            {
                // Get path to switch.
                var (legacyMapResult, switchPath, _) = MatchLegacyPath(travelParams.Path.Append(arrayItem.ToString()));

                // Add switch if it is not discarded.
                if (legacyMapResult != LegacyPathResult.Discard)
                    KeyValueFromPath(travelParams.TopGroup, switchPath.ToArray());
            }
        }
        // Key/value
        else
        {
            var (legacyMapResult, targetPath, linkState) = MatchLegacyPath(travelParams.Path);

            // If the setting is linked to a path, share the disabled state.
            if (linkState is not null && propValue.Type == JTokenType.Boolean)
            {
                var toggleKeyValue = KeyValueFromPath(travelParams.TopGroup, linkState.ToArray());
                toggleKeyValue.Disabled = !propValue.ToObject<bool>();
            }

            // Should be disarded?
            if (legacyMapResult == LegacyPathResult.Discard)
                return;

            // Preemptive check for false switches, which should be ignored.
            if (propValue.Type == JTokenType.Boolean)
            {
                // linkedSetting is the same as what "keyValue.Name.A" would have been down below.
                var linkedSetting = SettingsTraveller.Root().StepRange(targetPath).CurrentObject;
                var setTo = propValue.ToObject<bool>();

                if (linkedSetting?.Type is EObjectType.Switch && !setTo)
                    // This is a switch set to false, ignore it.
                    return;
            }

            var keyValue = KeyValueFromPath(travelParams.TopGroup!, targetPath.ToArray());
            ISettingValue? value = null;

            // Get value
            switch (propValue.Type)
            {
                // Assume floats are percentage numbers.
                case JTokenType.Float:
                    value = new NumberSettingValue(propValue.ToObject<double>(), true);
                    break;

                // Assume integers are percentages if the linked EObject is not known.
                case JTokenType.Integer:
                    value = new NumberSettingValue(propValue.ToObject<double>(),
                        keyValue.Name.A?.Type is null or EObjectType.Range);
                    break;

                // Usually refers to an option, except in the case of "main.Description" and
                // "main.Mode Name", which are actually strings.
                case JTokenType.String:
                    // Is expecting literal string value? (Description and Mode name)
                    if (keyValue.Name.A?.Type is EObjectType.String)
                    {
                        value = new StringSettingValue(propValue.ToString());
                    }
                    else // Otherwise, this is an option type.
                    {
                        value = new OptionSettingValue(propValue.ToString());
                    }

                    break;

                // On/Off, Enabled/Disabled, Yes/No, or switches.
                case JTokenType.Boolean:
                    bool set = propValue.ToObject<bool>();
                    // Is the boolean kind known?
                    if (keyValue.Name.A?.Type is EObjectType.OnOff or EObjectType.EnabledDisabled or EObjectType.YesNo)
                    {
                        value = new OptionSettingValue(keyValue.Name.A.Options[set ? 1 : 0]);
                    }
                    // Do nothing if this is a switch.
                    else if (keyValue.Name.A?.Type is not EObjectType.Switch)
                    {
                        // Fallback strategy
                        value = new OptionSettingValue(set ? "On" : "Off");
                    }
                    break;
            }

            keyValue.Value = value;
        }
    }

    static (LegacyPathResult Result, IEnumerable<string> Path, IEnumerable<string>? LinkState) MatchLegacyPath(IEnumerable<string> path)
    {
        var (result, newPath, linkState) = LobbySettings.Instance?.MapLegacy.MatchPath(path) ?? default;
        newPath ??= path;
        return (result, newPath, linkState);
    }

    static SettingKeyValue KeyValueFromPath(GroupSettingValue topGroup, string[] path)
    {
        var travel = SettingsTraveller.Root();

        for (int i = 0; ; i++)
        {
            travel = travel.Step(path[i]);
            var name = Variant.AElseB(travel.CurrentObject, path[i]);

            // Get the key/value
            var keyValue = topGroup.Get(name);
            if (keyValue is null)
            {
                keyValue = new(name, null);
                topGroup.Add(keyValue);
            }

            bool willBeMore = i < path.Length - 1;
            if (willBeMore)
            {
                if (keyValue.Value is not GroupSettingValue nextGroup)
                {
                    nextGroup = new();
                    // Replace current keyvalue
                    keyValue.Value = nextGroup;
                }
                topGroup = nextGroup;
            }
            else return keyValue;
        }
    }


    /// <summary>Assists in naviating the JSON.</summary>
    /// <param name="Path">The current object path.</param>
    /// <param name="TopGroup">Points to the root settings collection.</param>
    /// <param name="Diagnostics">The list where diagnostics are reported to.</param>
    record struct TravelParams(IEnumerable<string> Path, GroupSettingValue TopGroup, List<LegacyJsonDiagnostic> Diagnostics)
    {
        /// <summary>Takes another step in the json object path with the provided name.</summary>
        public readonly TravelParams Step(string name) => new(Path.Append(name), TopGroup, Diagnostics);
    }

    record struct LegacyJsonDiagnostic(string Message, LegacyJsonDiagnosticType DiagnosticType);

    enum LegacyJsonDiagnosticType
    {
        Error,
        Warning
    }
}