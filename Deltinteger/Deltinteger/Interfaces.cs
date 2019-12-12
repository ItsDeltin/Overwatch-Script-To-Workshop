using System.Text;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.WorkshopWiki;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

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
        bool WholeContext { get; }
        CompletionItem GetCompletion();
    }

    public interface ICallable
    {
        void Call(Location calledFrom);
    }

    public interface IParameterCallable
    {
        CodeParameter[] Parameters { get; }
        Location DefinedAt { get; }
    }

    public interface IGettable
    {
        IWorkshopTree GetVariable(Element eventPlayer = null);
    }
}