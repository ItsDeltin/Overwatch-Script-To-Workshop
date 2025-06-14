#nullable enable

using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Workshop;

namespace Deltin.Deltinteger.Parse
{
    public class SubroutineCollection(MetaElementSettings settings)
    {
        private List<Subroutine> Subroutines { get; } = new List<Subroutine>();
        private int CurrentID = 0;

        public Subroutine NewSubroutine(string name, int? id = null)
        {
            // Get the next available name.
            name = CompileIndexedElements.WorkshopNameFromCodeName(name, Subroutines.Select(sr => sr.Name).ToArray(), settings);

            if (id is null)
            {
                while (Subroutines.Any(subroutine => subroutine.ID == CurrentID))
                {
                    CurrentID++;
                }

                id = CurrentID;
                CurrentID++;
            }

            Subroutine newRoutine = new Subroutine(id.Value, name);
            Subroutines.Add(newRoutine);

            return newRoutine;
        }

        public void ToWorkshop(WorkshopBuilder builder)
        {
            if (Subroutines.Count == 0) return;

            builder.AppendKeywordLine("subroutines");
            builder.AppendLine("{");
            builder.Indent();

            foreach (Subroutine routine in Subroutines)
                builder.AppendLine(routine.ID.ToString() + ": " + routine.Name);

            builder.Outdent();
            builder.AppendLine("}");
            builder.AppendLine();
        }
    }
}