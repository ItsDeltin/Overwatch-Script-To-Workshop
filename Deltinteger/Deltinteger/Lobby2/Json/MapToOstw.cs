#nullable enable
using Newtonsoft.Json;

namespace Deltin.Deltinteger.Lobby2.Json;

class MapToOstw
{
    [JsonProperty("from")]
    public string? From { get; set; }

    [JsonProperty("to")]
    public string? To { get; set; }

    [JsonProperty("link_state")]
    public string? LinkState { get; set; }
}