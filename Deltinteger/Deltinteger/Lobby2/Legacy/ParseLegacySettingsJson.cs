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
            AnalyzeProperty(prop, travelParams.Step(prop.Name));
    }

    static void AnalyzeProperty(JProperty prop, TravelParams travelParams)
    {
        // Recursively check objects.
        if (prop.Value.Type == JTokenType.Object)
        {
            CheckObject((JObject)prop.Value, travelParams);
        }
        // Key/value
        else
        {
            var targetPath = MatchLegacyPath(travelParams.Path) ?? travelParams.Path;
            var keyValue = KeyValueFromPath(travelParams.TopGroup!, targetPath.ToArray());
            ISettingValue? value = null;

            // Get value
            switch (prop.Value.Type)
            {
                // Assume floats are percentage numbers.
                case JTokenType.Float:
                    value = new NumberSettingValue(prop.Value.ToObject<double>(), true);
                    break;

                // Assume integers are percentages if the linked EObject is not known.
                case JTokenType.Integer:
                    value = new NumberSettingValue(prop.Value.ToObject<double>(),
                        keyValue.Name.A?.Type is null or EObjectType.Range);
                    break;

                // Usually refers to an option, except in the case of "main.Description" and
                // "main.Mode Name", which are actually strings.
                case JTokenType.String:
                    // Is expecting literal string value? (Description and Mode name)
                    if (keyValue.Name.A?.Type is EObjectType.String)
                    {
                        value = new StringSettingValue(WorkshopStringUtility.WorkshopStringFromRawText(prop.Value.ToString()));
                    }
                    else // Otherwise, this is an option type.
                    {
                        value = new OptionSettingValue(prop.Value.ToString());
                    }

                    break;

                // On/Off, Enabled/Disabled, and Yes/No.
                case JTokenType.Boolean:
                    bool set = prop.Value.ToObject<bool>();
                    if (keyValue.Name.A?.Type is EObjectType.OnOff or EObjectType.EnabledDisabled or EObjectType.YesNo)
                    {
                        value = new OptionSettingValue(keyValue.Name.A.Options[set ? 1 : 0]);
                    }
                    else
                    {
                        // Fallback (not very cool)
                        value = new OptionSettingValue(set ? "On" : "Off");
                    }
                    break;
            }

            keyValue.Value = value;
        }
    }

    static IEnumerable<string>? MatchLegacyPath(IEnumerable<string> path)
    {
        return LobbySettings.Instance?.MapLegacy.MatchPath(path);
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