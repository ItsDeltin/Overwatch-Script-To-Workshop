using System.Collections.Generic;

namespace Deltin.Deltinteger.Elements
{
    public class WorkshopVariable : MetaElement
    {
        public bool IsGlobal { get; }

        public WorkshopVariable(bool isGlobal, int id, string name) : base(id, name)
        {
            IsGlobal = isGlobal;
        }

        public override bool EqualTo(IWorkshopTree other)
        {
            return base.EqualTo(other) && ((WorkshopVariable)other).IsGlobal == IsGlobal;
        }
    }
}