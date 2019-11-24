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

        public bool IsValidName()
        {
            for (int c = 0; c < Name.Length; c++)
                if (!ValidVariableCharacters.Contains(Name[c]))
                    return false;
            return true;
        }

        public bool IsValidID()
        {
            return 0 <= ID && ID < Constants.NUMBER_OF_VARIABLES;
        }
    }
}