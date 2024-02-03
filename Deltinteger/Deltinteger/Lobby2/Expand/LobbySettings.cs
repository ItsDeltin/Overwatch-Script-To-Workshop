#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby;
using Deltin.Deltinteger.Lobby2.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Deltin.Deltinteger.Lobby2.Expand;

class LobbySettings
{
    public static LobbySettings? Instance { get; private set; }

    public static void LoadFromJson(string json)
    {
        try
        {
            var deserialized = JsonConvert.DeserializeObject<SettingsSchemaJson>(json);
            if (deserialized is not null)
            {
                Instance = Expand(deserialized);
            }
        }
        catch (Exception ex)
        {
            // TODO: do someting with ex
            // awkward position due to this being kind of a class initializer
            Instance = new(Array.Empty<EObject>(), new());
        }
    }

    public EObject[] Root { get; }
    public LegacyMapList MapLegacy { get; }

    LobbySettings(EObject[] root, LegacyMapList mapLegacy)
    {
        Root = root;
        MapLegacy = mapLegacy;
    }

    /// <summary>Converts a `SettingsSchemaJson` to a `LobbySettings`</summary>
    public static LobbySettings Expand(SettingsSchemaJson top)
    {
        // Collect templates
        var templates = top.Templates ?? new();
        IList<SObject> repository = top.Repository ?? Array.Empty<SObject>();
        var context = new ExpandContext(templates, repository);

        // Get root items
        var root = Array.Empty<EObject>();
        if (top.Root is not null)
        {
            root = ExpandObjects(context, top.Root).ToArray();
        }

        return new(root, LegacyMapList.FromJson(top.MapLegacyJson));
    }

    static IEnumerable<EObject> ExpandObjects(ExpandContext context, IEnumerable<SObject>? objects)
    {
        if (objects is null)
            yield break;

        foreach (var obj in objects)
        {
            var (expanded, siblings) = ExpandObject(context, obj);
            yield return expanded;
            foreach (var sibling in siblings)
                yield return sibling;
        }
    }

    static (EObject Object, IEnumerable<EObject> Siblings) ExpandObject(ExpandContext context, SObject jsonObject)
    {
        // Find ref object in repository.
        SObject? refObject = null;
        if (jsonObject.Ref is not null)
            context.TryGetRef(jsonObject.Ref, out refObject);

        // Find template
        var template = context.GetTemplate(jsonObject.Template);

        // Create new EObject
        var expanded = new EObject(
            // Find name in json object, otherwise look at ref object
            name: context.FormatName(jsonObject.Name ?? refObject?.Name ?? "?"),
            id: jsonObject.Id,
            type: DetermineType(context, jsonObject, template),
            options: jsonObject.Options,
            def: jsonObject.Default);

        context = context.AddFormat("$name", expanded.Name);

        var siblings = Enumerable.Empty<EObject>();
        var content = Enumerable.Empty<EObject>();

        // Apply template.
        {
            var addObjects = EvaluateParams(context.SetParent(expanded), jsonObject, refObject, template);

            if (template is not null && template.Sibling)
            {
                siblings = addObjects;
            }
            else
            {
                content = addObjects;
            }
        }

        // Add content.
        if (jsonObject.Content is not null)
            content = content.Concat(ExpandObjects(context.SetParent(expanded), jsonObject.Content));

        // Insert maps
        if (jsonObject.InsertMaps is not null)
        {
            var modeName = context.FormatName(jsonObject.InsertMaps);
            content = content.Concat(LobbyMap.AllMaps
                .Where(map => map.GameModes.Contains(modeName))
                .Select(map => new EObject(map.GetWorkshopName(), EObjectType.Switch)));
        }

        // Insert heroes
        if (jsonObject.InsertHeroes && ElementRoot.Instance.TryGetEnum("Hero", out var heroes))
        {
            content = content.Concat(heroes.Members.Select(member => new EObject(member.Name, EObjectType.Switch)));
        }

        // Copy ref children.
        if (refObject is not null)
            content = content.Concat(ExpandObjects(context, refObject.Content));

        expanded.Children = content.ToArray();
        return (expanded, siblings);
    }

    static IEnumerable<EObject> EvaluateParams(ExpandContext context, SObject obj, SObject? refObject, Template? template)
    {
        // No template, no params.
        if (template is null)
            yield break;

        // Evaluate default content.
        if (template.Content is not null)
            foreach (var item in ExpandObjects(context, template.Content))
                yield return item;

        // Collect inputs from object and ref object.
        IEnumerable<KeyValuePair<string, JToken>> inputs = obj.Parameters ??
            Enumerable.Empty<KeyValuePair<string, JToken>>();

        if (refObject?.Parameters is not null)
        {
            inputs = inputs.Concat(refObject.Parameters);
        }

        // Add template params.
        if (template.Params is not null)
        {
            // Add keys
            foreach (var input in inputs)
                context = context.AddFormat(input.Key, input.Value.ToString());

            foreach (var param in template.Params)
            {
                // Find value in obj
                var inputValue = inputs.Cast<KeyValuePair<string, JToken>?>()
                    .FirstOrDefault(p => p!.Value.Key == $"${param.Key}");

                if (inputValue is null)
                {
                    if (param.Value.Else is not null)
                    {
                        foreach (var item in ExpandObjects(context, param.Value.Else))
                            yield return item;
                    }
                }
                else if (param.Value.Then is not null)
                {
                    var inputValueString = inputValue?.Value.ToString();
                    if (inputValueString is not null)
                    {
                        foreach (var item in ExpandObjects(context.AddFormat("$value", inputValueString), param.Value.Then))
                            yield return item;
                    }
                }
            }
        }
    }

    static EObjectType DetermineType(ExpandContext context, SObject obj, Template? template)
    {
        // Contains content, is a group.
        if (obj.Content is not null && obj.Content.Length > 0)
            return EObjectType.Group;

        if (template is not null && !template.Sibling && template.Content is not null && template.Content.Length > 0)
            return EObjectType.Group;

        if (obj.InsertMaps is not null || obj.InsertHeroes)
            return EObjectType.Group;

        if (obj.Options is not null && obj.Options.Length > 0)
            return EObjectType.Option;

        var refObject = context.GetRef(obj.Ref);
        Template? refTemplate = context.GetTemplate(refObject?.Template);

        return (obj.Type ?? template?.BaseType) switch
        {
            "boolean_onoff" => EObjectType.OnOff,
            "boolean_yesno" => EObjectType.YesNo,
            "boolean_enableddisabled" => EObjectType.EnabledDisabled,
            "range_percentage" => EObjectType.Range,
            "range_int" => EObjectType.Int,
            "switch" => EObjectType.Switch,
            "string" => EObjectType.String,
            _ => refObject is not null
                ? DetermineType(context, refObject, refTemplate)
                : EObjectType.Unknown
        };
    }
}