using System.Text;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.WorkshopWiki;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger
{
    public interface IWorkshopTree
    {
        string ToWorkshop();
        void DebugPrint(Log log, int depth = 0);
        double ServerLoadWeight();
    }

    public interface IMethod : ILanguageServerInfo
    {
        string Name { get; }
        ParameterBase[] Parameters { get; }
        WikiMethod Wiki { get; }
    }

    public interface ILanguageServerInfo
    {
        string GetLabel(bool markdown);
    }

    public interface ISkip
    {
        int SkipParameterIndex();
    }

    public interface IScopeable
    {
        string Name { get; }
        AccessLevel AccessLevel { get; }
        Node Node { get; }
    }

    public interface ITypeRegister
    {
        void RegisterParameters(ParsingData parser);
    }

    public interface ICallable
    {
        
    }
}