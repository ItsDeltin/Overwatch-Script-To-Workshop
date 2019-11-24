using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public interface INode
    {
        Location Location { get; }
    }

    public interface IDefine : INode
    {
        string VariableName { get; }
        string Type { get; }
        Node Value { get; }
        bool Extended { get; }
    }

    public interface IConstantSupport : INode
    {
        object GetValue();
    }

    public interface ICallableNode : INode
    {
        Node[] Parameters { get; }
    }

    public interface IImportNode : INode
    {
        string File { get; }
    }

    public interface IBlockContainer : INode
    {
        PathInfo[] Paths();
    }

    public class PathInfo
    {
        public BlockNode Block { get; }
        public Location ErrorRange { get; }
        public bool WillRun { get; }

        public PathInfo (BlockNode block, Location errorRange, bool willRun)
        {
            Block = block;
            ErrorRange = errorRange;
            WillRun = willRun;
        }
    }
}