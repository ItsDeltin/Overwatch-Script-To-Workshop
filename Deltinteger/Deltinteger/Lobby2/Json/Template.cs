#nullable enable

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.Lobby2.Json;

class Template
{
    [JsonProperty("params")]
    public Dictionary<string, TemplateParam>? Params { get; set; }

    [JsonProperty("content")]
    public SObject[]? Content { get; set; }
}

class TemplateParam
{
    [JsonProperty("then")]
    public SObject[]? Then { get; set; }

    [JsonProperty("else")]
    public SObject[]? Else { get; set; }
}