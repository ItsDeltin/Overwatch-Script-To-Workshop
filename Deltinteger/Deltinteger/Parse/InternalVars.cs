using System;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class InternalVar : IIndexReferencer
    {
        public string Name { get; }
        public AccessLevel AccessLevel { get; set; } = AccessLevel.Public;
        public Location DefinedAt => null;
        public bool WholeContext => true;
        public CompletionItemKind CompletionItemKind { get; set; } = CompletionItemKind.Variable;
        public string Detail { get; set; }
        public string Documentation { get; set; }
        public CodeType CodeType { get; set; }
        public bool IsSettable { get; set; } = true;
        public VariableType VariableType => VariableType.Global;

        public InternalVar(string name, CompletionItemKind completionItemKind = CompletionItemKind.Variable)
        {
            Name = name;
            CompletionItemKind = completionItemKind;
        }
        public InternalVar(string name, AccessLevel accessLevel, CompletionItemKind completionItemKind = CompletionItemKind.Variable)
        {
            Name = name;
            AccessLevel = accessLevel;
            CompletionItemKind = completionItemKind;
        }

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true) => throw new Exception("Cannot parse internal variables.");
        public virtual Scope ReturningScope() => CodeType?.ReturningScope();
        public virtual CodeType Type() => CodeType;

        public bool Settable() => IsSettable;

        public virtual void Call(ScriptFile script, DocRange callRange) {}

        public virtual CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind,
            Detail = Detail,
            Documentation = Extras.GetMarkupContent(Documentation)
        };
    }
}