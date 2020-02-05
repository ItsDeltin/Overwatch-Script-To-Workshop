using System;
using System.Collections.Generic;
using System.Linq;
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
            return newRoutine;
        }
    }
}