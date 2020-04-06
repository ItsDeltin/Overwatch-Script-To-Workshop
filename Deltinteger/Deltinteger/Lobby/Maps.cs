using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using Deltin.Deltinteger.Dump;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Deltin.Deltinteger.Lobby
{
    public class LobbyMap
    {
        public static LobbyMap[] AllMaps { get; } = GetMaps();
        public string Name { get; set; }
        public List<string> GameModes { get; set; }

        public LobbyMap() {}
        public LobbyMap(string name)
        {
            Name = name;
            GameModes = new List<string>();
        }
        
        private static LobbyMap[] GetMaps()
        {
            // Get the Maps.json file.
            string mapsFile = Path.Combine(Program.ExeFolder, "Maps.json");

            // Log and return empty if the file does not exist.
            if (!File.Exists(mapsFile))
            {
                Serilog.Log.Error("Maps.json not found at '" + mapsFile + "'.");
                return new LobbyMap[0];
            }

            // Read the Maps.json file.
            string json;
            try
            {
                json = File.ReadAllText(mapsFile);
            }
            catch (Exception ex)
            {
                // Log and return empty if the file failed to load.
                Serilog.Log.Error(ex, "Failed to load Maps.json.");
                return new LobbyMap[0];
            }

            // Deserialize Maps.json.
            try
            {
                return JsonConvert.DeserializeObject<LobbyMap[]>(json);
            }
            catch (Exception ex)
            {
                // Log and return empty if the file failed to deserialize.
                Serilog.Log.Error(ex, "Failed to deserialize Maps.json.");
                return new LobbyMap[0];
            }
        }

        public static void GetMaps(string datatoolPath, string overwatchPath, string outputFile)
        {
            // Get DataTool's list-maps output.
            DataTool dataTool = new DataTool(datatoolPath, overwatchPath);
            string[] mapOutput = dataTool.RunCommand("list-maps", "list_maps").Split("\r\n");

            MapParseState state = MapParseState.GettingMap; // Stores the current state of reading the output.
            List<LobbyMap> maps = new List<LobbyMap>(); // List of all maps.
            LobbyMap currentMap = null; // The current map being added.

            Regex mapName = new Regex("Name: (.*)");
            Regex variantName = new Regex("Name: (.*)");

            // Loop through each line.
            foreach (string line in mapOutput)
            {
                // If the current line is empty, set the state to getting the map and continue.
                if (line == "")
                {
                    state = MapParseState.GettingMap;
                    continue;
                }
                // DataTool info starts with [
                if (line[0] == '[') continue;

                // 'States' and 'GameModes' starts with 8 spaces.
                bool isList = line.StartsWith("        ");

                // If the current state is getting the map states and there are no more states, set the state to obtaining elements.
                if (state == MapParseState.States && !isList) state = MapParseState.Elements;
                else if (state == MapParseState.GameModes)
                {
                    // If the current state is getting the gamemodes and there are no more gamemodes, set the current state to getting elements.
                    // Otherwise, get the current gamemode.
                    if (!isList) state = MapParseState.Elements;
                    else currentMap.GameModes.Add(line.Substring(8, line.IndexOf('(') - 9));
                }

                if (state == MapParseState.GettingMap)
                {
                    // If the current state is getting the map, check if the current line contains a map name.
                    Match nameMatch = mapName.Match(line);
                    if (nameMatch.Success)
                    {
                        if (currentMap != null && IsValidMap(maps, currentMap)) maps.Add(currentMap);
                        currentMap = new LobbyMap(nameMatch.Groups[1].Value);
                        state = MapParseState.Elements;
                    }
                    continue;
                }
                if (state == MapParseState.Elements)
                {
                    // Get the map elements.
                    if (line == "    States:") state = MapParseState.States;
                    else if (line == "    GameModes:") state = MapParseState.GameModes;
                    else
                    {
                        Match variantNameMatch = variantName.Match(line);
                        if (variantNameMatch.Success) currentMap.Name = variantNameMatch.Groups[1].Value;
                    } 
                    continue;
                }
            }

            if (IsValidMap(maps, currentMap)) maps.Add(currentMap);
            Program.WorkshopCodeResult(JsonConvert.SerializeObject(maps, Formatting.Indented));
        }

        private static bool IsValidMap(List<LobbyMap> maps, LobbyMap newMap) => !maps.Any(map => map.Name == newMap.Name) && newMap.GameModes.Count > 0;

        enum MapParseState
        {
            GettingMap,
            Elements,
            States,
            GameModes
        }
    }
}