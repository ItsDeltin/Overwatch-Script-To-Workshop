#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Emulator;

/// <summary>Represents an emulated player.</summary>
public class PlayerInformation(string name, bool isHost)
{
    /// <summary>The name of the player in the emulator.</summary>
    public string Name { get; } = name;
    /// <summary>The unique emulation value referencing this player.</summary>
    public EmulateValue.Player Value { get; } = new(name);
    /// <summary>Is this player the host of the game?</summary>
    public bool IsHost { get; } = isHost;
    /// <summary>The player's personal variable set (Player Variables)</summary>
    public EmulateVariableSet Variables { get; } = new();
}

/// <summary>Holds a list of emulated players.</summary>
public class PlayerList
{
    readonly List<PlayerInformation> players = [];

    /// <summary>Finds a player in the emulation using it's unique emulation value.</summary>
    public PlayerInformation? GetPlayerInformationFromValue(EmulateValue value)
        => players.FirstOrDefault(p => p.Value == value);

    /// <summary>Finds a player by it's name.</summary>
    public PlayerInformation? GetPlayerInformationFromName(string name)
        => players.FirstOrDefault(p => p.Value.Name == name);

    /// <summary>Adds a player to the emulation.</summary>
    public PlayerInformation AddPlayer(string name, bool isHost)
    {
        var instance = new PlayerInformation(name, isHost);
        players.Add(instance);
        return instance;
    }

    /// <summary>Finds the simulated host of the game.</summary>
    public EmulateValue? GetHostValue() => players.FirstOrDefault(p => p.IsHost)?.Value;
}