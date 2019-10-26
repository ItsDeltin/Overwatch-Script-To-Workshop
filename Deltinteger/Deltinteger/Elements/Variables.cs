using System;
using System.Linq;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Elements
{
    public class WorkshopVariable : IWorkshopTree
    {
        public static readonly char[] ValidVariableCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray();
        
        public int ID { get; }
        public string Name { get; }
        public bool IsGlobal { get; }

        public WorkshopVariable(bool isGlobal, int id, string name)
        {
            // for (int c = 0; c < name.Length; c++)
            //     if (!ValidVariableCharacters.Contains(name[c]))
            //         throw new ArgumentOutOfRangeException(nameof(name), name, "Variable name contains invalid character '" + name[c] + "'.");

            // if (id < 0 || id > Constants.NUMBER_OF_VARIABLES)
            //     throw new ArgumentOutOfRangeException(nameof(id), "Variable ID cannot be lower than 0 or higher than " + Constants.NUMBER_OF_VARIABLES + ".");
            
            ID = id;
            Name = name;
            IsGlobal = isGlobal;
        }

        public string ToWorkshop()
        {
            return Name;
        }

        public void DebugPrint(Log log, int depth)
        {
            log.Write(LogLevel.Verbose, Extras.Indent(depth, false) + Name);
        }
    }
}