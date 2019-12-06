using System.Text;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.WorkshopWiki;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger
{
    public interface IWorkshopTree
    {
        string ToWorkshop();
        void DebugPrint(Log log, int depth = 0);
    }

    public interface IMethod : IScopeable, ILanguageServerInfo
    {
        Parse.CodeParameter[] Parameters { get; }
        WikiMethod Wiki { get; }
        CodeType ReturnType { get; }
        IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] values);
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
        Location DefinedAt { get; }
        string ScopeableType { get; }
    }

    public interface ICallable
    {
        void Call(Location calledFrom);
    }
}