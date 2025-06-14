#nullable enable

using System;
using System.Linq;

namespace Deltin.Deltinteger.Parse.Workshop;

static class CompileIndexedElements
{
    public static readonly char[] ValidVariableCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray();

    public static string WorkshopNameFromCodeName(string name, string[] takenNames, MetaElementSettings options)
    {
        var newName = string.Empty;
        bool needsPrefix = options.Prefix is not null && !name.StartsWith(options.Prefix);

        // Remove invalid characters and replace ' ' with '_'.
        for (int i = 0; i < name.Length; i++)
            if (name[i] == ' ')
                newName += '_';
            else if (ValidVariableCharacters.Contains(name[i]))
                newName += name[i];

        // Add prefix if provided
        if (needsPrefix)
            newName = options.Prefix + newName;

        // Trim string length
        if (newName.Length > Constants.MAX_VARIABLE_NAME_LENGTH)
            newName = newName[..Constants.MAX_VARIABLE_NAME_LENGTH];

        // Add a number to the end of the variable name if a variable with the same name was already created.
        if (NameTaken(newName, takenNames))
        {
            int num = 0;
            while (NameTaken(NewName(newName, num), takenNames)) num++;
            newName = NewName(newName, num);
        }
        return newName.ToString();
    }

    private static bool NameTaken(string name, string[] takenNames)
    {
        return takenNames.Contains(name);
    }

    private static string NewName(string baseName, int indent)
    {
        return baseName.Substring(0, Math.Min(baseName.Length, Constants.MAX_VARIABLE_NAME_LENGTH - (indent.ToString().Length + 1))) + "_" + indent;
    }
}

public record struct MetaElementSettings(string? Prefix);