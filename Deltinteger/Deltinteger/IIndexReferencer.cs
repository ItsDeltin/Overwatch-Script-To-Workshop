using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger
{
    public interface IIndexReferencer : IVariable, IExpression, ICallable, ILabeled
    {
        bool Settable();
        VariableType VariableType { get; }
    }

    public abstract class IndexReferencer : IIndexReferencer
    {
        public string Name { get; }
        public VariableType VariableType { get; protected set; }
        public bool Static { get; protected set; }
        public bool WholeContext { get; protected set; } = true;
        public LanguageServer.Location DefinedAt { get; protected set; }
        public AccessLevel AccessLevel { get; protected set; } = AccessLevel.Public;
        public CodeType CodeType { get; protected set; }
        public MarkupBuilder Documentation { get; set; }

        public IndexReferencer(string name)
        {
            Name = name;
        }

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            if (DefinedAt != null) parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            parseInfo.Script.AddHover(callRange, GetLabel(true));
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new Location(parseInfo.Script.Uri, callRange));
        }

        public CompletionItem GetCompletion() => new CompletionItem()
        {
            Label = Name,
            Kind = CompletionItemKind.Variable,
            Detail = (CodeType?.GetName() ?? "define") + " " + Name,
            Documentation = Documentation == null ? null : Extras.GetMarkupContent(Documentation.ToString())
        };

        public string GetLabel(bool markdown)
        {
            string typeName = "define";
            if (CodeType != null) typeName = CodeType.GetName();
            if (markdown) return HoverHandler.Sectioned(typeName + " " + Name, Documentation?.ToString(true));
            else return typeName + " " + Name;
        }

        public IWorkshopTree Parse(ActionSet actionSet) => throw new NotImplementedException();
        public virtual bool Settable() => true;
        public Scope ReturningScope() => CodeType.GetObjectScope();
        public CodeType Type() => CodeType;
    }
}