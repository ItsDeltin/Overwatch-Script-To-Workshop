#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
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
            // TODO: handle bad json
        }
    }

    public EObject[] Root { get; }

    LobbySettings(EObject[] root)
    {
        Root = root;
    }

    public static LobbySettings Expand(SettingsSchemaJson top)
    {
        // Collect templates
        var templates = top.Templates ?? new();
        var repository = new List<EObject>();
        var context = new ExpandContext(templates, repository);

        // Expand repository
        if (top.Repository is not null)
        {
            repository.AddRange(ExpandObjects(context, top.Repository));
        }

        // Get root items
        var root = Array.Empty<EObject>();
        if (top.Root is not null)
        {
            root = ExpandObjects(context, top.Root).ToArray();
        }

        return new(root);
    }

    static IEnumerable<EObject> ExpandObjects(ExpandContext context, IEnumerable<SObject> objects)
    {
        foreach (var obj in objects)
            yield return ExpandObject(context, obj);
    }

    static EObject ExpandObject(ExpandContext context, SObject jsonObject)
    {
        var expanded = new EObject(jsonObject.Name ?? "?", jsonObject.Id);

        // Apply template.
        var content = EvaluateParams(context.SetParent(expanded), jsonObject);

        // Add content.
        if (jsonObject.Content is not null)
            content = content.Concat(ExpandObjects(context.SetParent(expanded), jsonObject.Content));

        // Copy ref
        if (jsonObject.Ref is not null && context.TryGetRef(jsonObject.Ref, out var refObject))
        {
            content = content.Concat(refObject.Children);
        }

        expanded.Children = content.ToArray();
        return expanded;
    }

    static IEnumerable<EObject> EvaluateParams(ExpandContext context, SObject obj)
    {
        // No template, no params.
        if (obj.Template is null)
            yield break;

        var template = context.GetTemplate(obj.Template);

        // Template name not found.
        if (template is null)
            yield break;

        // Evaluate default content.
        if (template.Content is not null)
            foreach (var item in ExpandObjects(context, template.Content))
                yield return item;

        // Add template params.
        if (template.Params is not null)
        {
            foreach (var param in template.Params)
            {
                // Find value in obj
                var inputValue = obj.Parameters?
                    .Cast<KeyValuePair<string, JToken>?>()
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
                        foreach (var item in ExpandObjects(context, param.Value.Then))
                            yield return item;
                    }
                }
            }
        }
    }
}