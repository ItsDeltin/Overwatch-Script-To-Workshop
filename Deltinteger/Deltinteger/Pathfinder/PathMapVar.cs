using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathMapVar : Var
    {
        public PathMap PathMap { get; }
        public IndexedVar Nodes { get; }
        public IndexedVar Segments { get; }

        public PathMapVar(ParsingData parser, string name, ScopeGroup scope, Node node, PathMap pathMap) : base(name, scope, node)
        {
            PathMap = pathMap;
            Nodes = parser.VarCollection.AssignVar(null, "Path Map Node Data", true, null);
            Segments = parser.VarCollection.AssignVar(null, "Path Map Segment Data", true, null);
            Setup(parser);
        }

        override public Element GetVariable(Element targetPlayer = null)
        {
            throw new NotImplementedException();
        }

        override public bool Gettable() => false;
        override public bool Settable() => false;

        private void Setup(ParsingData parser)
        {
            parser.GlobalSetup(Nodes.SetVariable(PathMap.NodesAsWorkshopData()));
            parser.GlobalSetup(Segments.SetVariable(PathMap.SegmentsAsWorkshopData()));
        }
    }
}