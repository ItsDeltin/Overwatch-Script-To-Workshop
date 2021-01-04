using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    interface IPathfinderInfo
    {
        ActionSet ActionSet { get; }
        Element InitialNode { get; }
        Element NodeArray { get; }
        Element SegmentArray { get; }
        Element AttributeArray { get; }
        Element LoopCondition { get; }
        void OnLoop();
        void OnConnectLoop();
        void OnLoopEnd();
        void Finished();
    }
}