using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class InternalVar : IIndexReferencer, IAmbiguityCheck
    {
        public string Name { get; }
        public AccessLevel AccessLevel { get; set; } = AccessLevel.Public;
        public Location DefinedAt { get; set; }
        public bool WholeContext => true;
        public CompletionItemKind CompletionItemKind { get; set; } = CompletionItemKind.Variable;
        public string Documentation { get; set; }
        public CodeType CodeType { get; set; }
        public bool IsSettable { get; set; } = true;
        public VariableType VariableType { get; set; } = VariableType.Global;
        public bool Static => true;
        public TokenType? TokenType { get; set; } = null;
        public bool Ambiguous { get; set; }
        public bool RequiresCapture => false;

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
        public InternalVar(string name, CodeType type, CompletionItemKind completionItemKind = CompletionItemKind.Variable)
        {
            Name = name;
            CodeType = type;
            CompletionItemKind = completionItemKind;
        }

        public virtual IWorkshopTree Parse(ActionSet actionSet) => throw new Exception("Cannot parse internal variables.");
        public virtual Scope ReturningScope() => CodeType?.ReturningScope();
        public virtual CodeType Type() => CodeType;

        public bool Settable() => IsSettable;

        public virtual void Call(ParseInfo parseInfo, DocRange callRange)
        {
            parseInfo.Script.AddHover(callRange, GetLabel(true));
            if (TokenType != null) parseInfo.Script.AddToken(callRange, (TokenType)TokenType);
        }

        public virtual CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind,
            Detail = GetLabel(false),
            Documentation = Extras.GetMarkupContent(Documentation)
        };

        public virtual string GetLabel(bool markdown)
        {
            string typeName = "define";
            if (CodeType != null) typeName = CodeType.GetName();
            if (markdown) return HoverHandler.Sectioned(typeName + " " + Name, Documentation);
            else return typeName + " " + Name;
        }

        bool IAmbiguityCheck.CanBeAmbiguous() => Ambiguous;
    }
}