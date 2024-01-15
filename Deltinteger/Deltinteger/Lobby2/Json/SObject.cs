#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Deltin.Deltinteger.Lobby2.Json;

class SObject
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("ref")]
    public string? Ref { get; set; }

    [JsonProperty("template")]
    public string? Template { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("default")]
    public object? Default { get; set; }

    [JsonProperty("content")]
    public SObject[]? Content { get; set; }

    [JsonProperty("options")]
    public string[]? Options { get; set; }

    [JsonProperty("min")]
    public double Min { get; set; } = 10;

    [JsonProperty("max")]
    public double Max { get; set; } = 500;

    [JsonExtensionData]
    public IDictionary<string, JToken>? Parameters { get; set; }
}