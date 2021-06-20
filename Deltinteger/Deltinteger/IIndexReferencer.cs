using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger
{
    public interface IIndexReferencer : IVariable, IExpression, ICallable
    {
        bool Settable();
        bool RequiresCapture { get; }
        VariableType VariableType { get; }
        bool InExtendedCollection => false;
        int ID => -1;
        bool Recursive => false;
    }

    public class IndexReferencer : IIndexReferencer
    {
        public string Name { get; }
        public VariableType VariableType { get; protected set; }
        public bool Static { get; protected set; }
        public bool WholeContext { get; protected set; } = true;
        public LanguageServer.Location DefinedAt { get; protected set; }
        public AccessLevel AccessLevel { get; protected set; } = AccessLevel.Public;
        public CodeType CodeType { get; protected set; }
        public MarkupBuilder Documentation { get; set; }
        public bool RequiresCapture => false;
        ICodeTypeSolver IScopeable.CodeType => CodeType;

        public IndexReferencer(string name)
        {
            Name = name;
        }

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            if (DefinedAt != null) parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            parseInfo.Script.AddHover(callRange, ((ILabeled)this).GetLabel(parseInfo.TranslateInfo, LabelInfo.Hover));
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new Location(parseInfo.Script.Uri, callRange));
        }
        
        public IWorkshopTree Parse(ActionSet actionSet) => throw new NotImplementedException();
        public virtual bool Settable() => true;
        public Scope ReturningScope() => CodeType.GetObjectScope();
        public CodeType Type() => CodeType;
    }
}