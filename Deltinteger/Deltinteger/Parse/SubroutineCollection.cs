using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class SubroutineCollection
    {
        private List<Subroutine> Subroutines { get; } = new List<Subroutine>();
        private int CurrentID = 0;

        public Subroutine NewSubroutine(string name)
        {
            // Get the next available name.
            name = MetaElement.WorkshopNameFromCodeName(name, Subroutines.Select(sr => sr.Name).ToArray());

            Subroutine newRoutine = new Subroutine(CurrentID, name);
            CurrentID++;
            Subroutines.Add(newRoutine);
            
            return newRoutine;
        }

        public void ToWorkshop(StringBuilder stringBuilder, OutputLanguage language)
        {
            if (Subroutines.Count == 0) return;

            stringBuilder.AppendLine(I18n.I18n.Translate(language, "subroutines"));
            stringBuilder.AppendLine("{");

            foreach (Subroutine routine in Subroutines)
            {
                stringBuilder.AppendLine(Extras.Indent(1, false) +
                    routine.ID.ToString() + ": " + routine.Name
                );
            }

            stringBuilder.AppendLine("}");
            stringBuilder.AppendLine();
        }
    }
}