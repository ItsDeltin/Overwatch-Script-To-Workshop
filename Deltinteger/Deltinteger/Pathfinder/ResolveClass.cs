using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathResolveClass : ClassType
    {
        /// <summary>An array of numbers where each value is that index's parent index. Following the path will lead to the source. Subtract value by -1 since 0 is used for unset.</summary>
        public ObjectVariable ParentArray { get; private set; }
        /// <summary>The attributes of the parents.</summary>
        public ObjectVariable ParentAttributeArray { get; private set; }
        /// <summary>A reference to the source pathmap.</summary>
        public ObjectVariable Pathmap { get; private set; }
        /// <summary>A vector determining the destination.</summary>
        public ObjectVariable Destination { get; private set; }

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
            ParentAttributeArray = AddObjectVariable(new InternalVar("ParentAttributeArray"));
            
            // Set Pathmap
            Pathmap = AddObjectVariable(new InternalVar("OriginMap"));

            // Set Destination
            Destination = AddObjectVariable(new InternalVar("Destination"));

            serveObjectScope.AddNativeMethod(PathfindFunction());
        }

        private FuncMethod PathfindFunction() => new FuncMethod(new FuncMethodBuilder() {
            Name = "Pathfind",
            Documentation = "Pathfinds the specified players.",
            Parameters = new CodeParameter[] {
                new CodeParameter("players", "The players to pathfind.")
            },
            Action = (actionSet, call) => {
                // Get the ResolveInfoComponent.
                ResolveInfoComponent resolveInfo = actionSet.Translate.DeltinScript.GetComponent<ResolveInfoComponent>();

                // For each of the players, get the current.
                resolveInfo.Pathfind(actionSet, (Element)call.ParameterValues[0], Pathmap.Get(actionSet), ParentArray.Get(actionSet), ParentAttributeArray.Get(actionSet), Destination.Get(actionSet));

                return null;
            }
        });
    }
}