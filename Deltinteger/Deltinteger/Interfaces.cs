using System;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger
{
    public interface IElementProvider
    {
        IScopeable AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker);
        void AddDefaultInstance(IScopeAppender scopeAppender);
    }

    public interface INamed
    {
        string Name { get; }
    }

    public interface IScopeable : INamed, IAccessable
    {
        ICodeTypeSolver CodeType { get; }
        bool WholeContext { get; }
        CompletionItem GetCompletion(DeltinScript deltinScript);
    }

    public interface ICallable : INamed
    {
        void Call(ParseInfo parseInfo, DocRange callRange);
    }

    public interface IParameterCallable : ILabeled, IAccessable
    {
        CodeParameter[] Parameters { get; }
        MarkupBuilder Documentation { get; }
        object Call(ParseInfo parseInfo, DocRange callRange) => null;
        bool RestrictedValuesAreFatal => true;
    }

    public interface IAccessable
    {
        Location DefinedAt { get; }
        AccessLevel AccessLevel { get; }
    }

    public interface IGettable
    {
        bool CanBeSet();
        IWorkshopTree GetVariable(Element eventPlayer = null);
        void Set(ActionSet actionSet, IWorkshopTree value) => Set(actionSet, value, null, null);
        void Set(ActionSet actionSet, IWorkshopTree value, Element target, params Element[] index);
        void Modify(ActionSet actionSet, Operation operation, IWorkshopTree value, Element target, params Element[] index);
        IGettable ChildFromClassReference(IWorkshopTree reference);
    }

    public interface ILabeled
    {
        MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo);
    }

    public interface IBlockListener
    {
        void OnBlockApply(IOnBlockApplied onBlockApplied);
    }

    public interface IOnBlockApplied
    {
        void Applied();
    }

    public interface IWorkshopInit
    {
        void WorkshopInit(DeltinScript deltinScript);
    }
}