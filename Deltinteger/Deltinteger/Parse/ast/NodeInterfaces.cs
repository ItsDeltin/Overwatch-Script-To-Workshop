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
}