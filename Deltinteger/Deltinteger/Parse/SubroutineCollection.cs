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

        public void ToWorkshop(WorkshopBuilder builder) => new SubroutineSet(Subroutines.ToArray()).ToWorkshop(builder);
    }
}