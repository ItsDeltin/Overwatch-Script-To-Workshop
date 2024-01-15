#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.Lobby2.Json;

class SettingsSchemaJson
{
    [JsonProperty("repository")]
    public SObject[]? Repository { get; set; }

    [JsonProperty("root")]
    public SObject[]? Root { get; set; }

    [JsonProperty("templates")]
    public Dictionary<string, Template>? Templates { get; set; }
}