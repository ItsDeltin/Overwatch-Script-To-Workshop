#nullable enable
using Newtonsoft.Json;

namespace Deltin.Deltinteger.Lobby2.Json;

class MapToOstw
{
    [JsonProperty("from")]
    public string From { get; set; }

    [JsonProperty("to")]
    public string To { get; set; }
}

class Condition
{
    [JsonProperty("param")]
    public string Param { get; set; }

    [JsonProperty("type_is_not")]
    public string TypeIsNot { get; set; }
}