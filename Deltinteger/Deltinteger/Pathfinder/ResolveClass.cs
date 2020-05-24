using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathResolveClass : ClassType
    {
        public PathResolveClass Instance { get; } = new PathResolveClass();
        public ObjectVariable ParentArray { get; private set; }
        public ObjectVariable ParentAttributeArray { get; private set; }

        public PathResolveClass() : base("PathResolve")
        {

        }

        public override void ResolveElements()
        {
            if (elementsResolved) return;
            base.ResolveElements();

            // Set ParentArray
            ParentArray = AddObjectVariable(new InternalVar("ParentArray"));

            // Set ParentAttributeArray
            ParentAttributeArray = AddObjectVariable(new InternalVar("ParentArray"));
        }
    }
}