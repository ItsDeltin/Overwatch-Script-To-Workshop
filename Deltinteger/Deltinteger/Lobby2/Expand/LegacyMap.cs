#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Lobby2.Json;
using Deltin.Deltinteger.Model;

namespace Deltin.Deltinteger.Lobby2.Expand;

readonly struct LegacyMapList
{
    readonly LegacyMap[] legacyMaps = Array.Empty<LegacyMap>();

    public LegacyMapList(LegacyMap[] legacyMaps)
    {
        this.legacyMaps = legacyMaps;
    }

    public LegacyMapFormat MatchPath(IEnumerable<string> path)
    {
        foreach (var map in legacyMaps)
        {
            var format = map.Format(path);
            if (format.Result != LegacyPathResult.NoMatch)
                return format;
        }
        return default;
    }

    public static LegacyMapList FromJson(MapToOstw[]? legacyMapJsons)
    {
        return new(EnumerateLegacyMapJsons(legacyMapJsons).ToArray());
    }

    static IEnumerable<LegacyMap> EnumerateLegacyMapJsons(MapToOstw[]? legacyMapJsons)
    {
        if (legacyMapJsons is null)
            yield break;

        foreach (var legacyMapJson in legacyMapJsons)
        {
            var map = LegacyMap.From(legacyMapJson);
            if (map is not null)
                yield return map;
        }
    }
}

enum LegacyPathResult
{
    NoMatch,
    Discard,
    OverridePath
}

class LegacyMap
{
    readonly LegacyMapPath from;
    readonly LegacyMapPath? to;
    readonly LegacyMapPath? linkState;

    LegacyMap(LegacyMapPath from, LegacyMapPath? to, LegacyMapPath? linkState)
    {
        this.from = from;
        this.to = to;
        this.linkState = linkState;
    }

    public LegacyMapFormat Format(IEnumerable<string> path)
    {
        var match = from.Match(path);

        if (match is not null)
        {
            var link = linkState?.Format(match.Value);
            if (to is null)
                return new(LegacyPathResult.Discard, null, link);
            else
                return new(LegacyPathResult.OverridePath, to.Value.Format(match.Value), link);
        }
        return default;
    }

    public static LegacyMap? From(MapToOstw jsonMap)
    {
        var from = Compartmentalize(jsonMap.From);
        var to = Compartmentalize(jsonMap.To);
        var linkState = Compartmentalize(jsonMap.LinkState);

        if (from is not null)
            return new LegacyMap(from.Value, to, linkState);
        return null;
    }

    static LegacyMapPath? Compartmentalize(string? input)
    {
        if (input == null)
            return null;

        var parts = new List<LegacyMapPathPart>();
        foreach (var part in input.Split('.'))
        {
            // Format
            if (part.Length > 0 && part.StartsWith('$') && int.TryParse(part[1..], out int format))
                parts.Add(new(format));
            // String
            else
                parts.Add(new(part));
        }
        return new(parts);
    }
}

readonly record struct LegacyMapFormat(LegacyPathResult Result, IEnumerable<string>? NewPath, IEnumerable<string>? LinkState);

/// <summary>Used with both the 'from' and 'to' parameters.</summary>
readonly record struct LegacyMapPath(IReadOnlyList<LegacyMapPathPart> Parts)
{
    public readonly LegacyMapMatch? Match(IEnumerable<string> path)
    {
        // Input size is too small
        if (path.Count() < Parts.Count)
            return null;

        var formats = new Dictionary<int, string>();

        foreach (var (step, i) in path.WithIndex())
        {
            if (i >= Parts.Count) break;

            if (Parts[i].PathOrFormat.Get(out var key, out var format))
            {
                // Check key
                if (step != key)
                    return null;
            }
            else
            {
                // format
                formats.Add(format, step);
            }
        }

        return new(formats, path.Skip(Parts.Count));
    }

    public readonly IEnumerable<string> Format(LegacyMapMatch match)
    {
        foreach (var part in Parts)
        {
            if (part.PathOrFormat.Get(out var key, out var format))
            {
                // key
                yield return key;
            }
            // format
            else if (match.Formats.TryGetValue(format, out var formatTo))
            {
                yield return formatTo;
            }
            // Unknown
            else yield return "?";
        }

        foreach (var tail in match.Tail)
            yield return tail;
    }
}

/// <summary>One part in a dot path: A.$0.B</summary>
readonly record struct LegacyMapPathPart(Variant<string, int> PathOrFormat);

readonly record struct LegacyMapMatch(
    IReadOnlyDictionary<int, string> Formats,
    IEnumerable<string> Tail
);