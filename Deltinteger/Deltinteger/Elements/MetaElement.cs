using System;
using System.Linq;
using System.Text;

namespace Deltin.Deltinteger.Elements
{
    public abstract class MetaElement : IWorkshopTree
    {
        public int ID { get; }
        public string Name { get; }

        protected MetaElement(int id, string name)
        {
            ID = id;
            Name = name;
        }

        public virtual void ToWorkshop(WorkshopBuilder b, ToWorkshopContext context) => b.Append(Name);
        public virtual bool EqualTo(IWorkshopTree other)
        {
            if (this.GetType() != other.GetType()) return false;

            WorkshopVariable bAsMeta = (WorkshopVariable)other;
            return Name == bAsMeta.Name && ID == bAsMeta.ID;
        }

        public override string ToString() => Name;
    }
}