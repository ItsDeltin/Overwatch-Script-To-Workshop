using System;
using System.Linq;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Elements
{
    public abstract class MetaElement
    {
        public static readonly char[] ValidVariableCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray();
        public int ID { get; }
        public string Name { get; }

        protected MetaElement(int id, string name)
        {
            ID = id;
            Name = name;
        }
    }

    public class WorkshopVariable : MetaElement, IWorkshopTree
    {
        public bool IsGlobal { get; }

        public WorkshopVariable(bool isGlobal, int id, string name) : base(id, name)
        {            
            IsGlobal = isGlobal;
        }

        public string ToWorkshop(OutputLanguage language)
        {
            return Name;
        }

        public bool EqualTo(IWorkshopTree b)
        {
            if (this.GetType() != b.GetType()) return false;
            
            WorkshopVariable bAsVariable = (WorkshopVariable)b;
            return Name == bAsVariable.Name && IsGlobal == bAsVariable.IsGlobal;
        }
    }
}