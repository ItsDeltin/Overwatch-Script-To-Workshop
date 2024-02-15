using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Deltin.Deltinteger.Dump;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.Lobby
{
    public class LobbyMap
    {
        public static LobbyMap[] AllMaps { get; private set; }

        [JsonProperty("Name")]
        public string Name { get; set; }
        [JsonProperty("Workshop")]
        public string Workshop { get; set; }
        [JsonProperty("GameModes")]
        public string[] GameModes { get; set; } = [];

        public LobbyMap() { }

        public string GetWorkshopName() => Workshop ?? Name;

        public static void LoadFromJson(string json)
        {
            // Deserialize Maps.json.
            try
            {
                AllMaps = JsonConvert.DeserializeObject<LobbyMap[]>(json);
            }
            catch (Exception ex)
            {
                // Log and return empty if the file failed to deserialize.
                ErrorReport.Add("Failed to deserialize maps json: " + ex);
                AllMaps = [];
            }
        }
    }
}