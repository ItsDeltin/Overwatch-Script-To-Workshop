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

        public string ToWorkshop(OutputLanguage language)
        {
            return Name;
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

        public bool EqualTo(IWorkshopTree b)
        {
            if (this.GetType() != b.GetType()) return false;
            
            WorkshopVariable bAsVariable = (WorkshopVariable)b;
            return Name == bAsVariable.Name && IsGlobal == bAsVariable.IsGlobal;
        }
    }
}