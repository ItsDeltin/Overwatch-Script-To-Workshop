namespace Deltin.Deltinteger.Elements
{
    public class Subroutine : MetaElement
    {
        public Subroutine(int id, string name) : base(id, name) { }

        public override bool EqualTo(IWorkshopTree other)
        {
            return base.EqualTo(other);
        }
    }
}