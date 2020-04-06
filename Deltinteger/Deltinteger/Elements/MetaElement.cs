using System;
using System.Linq;
using System.Text;

namespace Deltin.Deltinteger.Elements
{
    public abstract class MetaElement : IWorkshopTree
    {
        public static readonly char[] ValidVariableCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray();
        public int ID { get; }
        public string Name { get; }

        protected MetaElement(int id, string name)
        {
            ID = id;
            Name = name;
        }

        public virtual string ToWorkshop(OutputLanguage language) => Name;
        public virtual bool EqualTo(IWorkshopTree other)
        {
            if (this.GetType() != other.GetType()) return false;
            
            WorkshopVariable bAsMeta = (WorkshopVariable)other;
            return Name == bAsMeta.Name && ID == bAsMeta.ID;
        }
        public abstract int ElementCount(int depth);

        public static string WorkshopNameFromCodeName(string name, string[] takenNames)
        {
            StringBuilder valid = new StringBuilder();

            // Remove invalid characters and replace ' ' with '_'.
            for (int i = 0; i < name.Length; i++)
                if (name[i] == ' ')
                    valid.Append('_');
                else if (MetaElement.ValidVariableCharacters.Contains(name[i]))
                    valid.Append(name[i]);
                
            string newName = valid.ToString();

            if (newName.Length > Constants.MAX_VARIABLE_NAME_LENGTH)
                newName = newName.Substring(0, Constants.MAX_VARIABLE_NAME_LENGTH);

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
}