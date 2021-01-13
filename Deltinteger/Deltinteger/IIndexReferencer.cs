using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger
{
    public interface IVariable : IElementProvider
    {
        string Name { get; }
        CodeType CodeType { get; }
        bool RequiresCapture => false;
        VariableType VariableType { get; }
        IVariableInstance GetInstance(InstanceAnonymousTypeLinker genericsLinker);
        IVariableInstance GetDefaultInstance();
    }

    public interface IVariableInstance : IScopeable
    {
        bool CanBeIndexed => true;
        IVariable Provider { get; }
        MarkupBuilder Documentation { get; }
        IGettableAssigner GetAssigner();
        IWorkshopTree ToWorkshop(ActionSet actionSet) => actionSet.IndexAssigner.Get(Provider).GetVariable();
        ICallVariable GetExpression(ParseInfo parseInfo, DocRange callRange, IExpression[] index, CodeType[] typeArgs) => new CallVariableAction(parseInfo.TranslateInfo.Types, this, index);
        void Call(ParseInfo parseInfo, DocRange callRange) => Call(this, parseInfo, callRange);
        MarkupBuilder GetLabel() => new MarkupBuilder().StartCodeLine().Add(CodeType.GetNameOrAny() + " " + Name).EndCodeLine();

        static CompletionItem GetCompletion(IVariableInstance variable, CompletionItemKind kind) => new CompletionItem()
        {
            Label = variable.Name,
            Kind = CompletionItemKind.Variable,
            Detail = variable.CodeType.GetNameOrAny() + " " + variable.Name,
            Documentation = variable.Documentation == null ? null : variable.Documentation.ToMarkup()
        };
        
        static void Call(IVariableInstance variable, ParseInfo parseInfo, DocRange callRange)
        {
            parseInfo.Script.AddHover(callRange, variable.GetLabel().ToString());
        }
    }

    public interface ICallVariable : IExpression
    {
        void Accept();
    }

    public class InternalVar : IVariable, IVariableInstance, IAmbiguityCheck
    {
        public string Name { get; }
        public VariableType VariableType { get; set; }
        public bool Static { get; set; }
        public bool WholeContext { get; set; } = true;
        public LanguageServer.Location DefinedAt { get; set; }
        public AccessLevel AccessLevel { get; set; } = AccessLevel.Public;
        public CodeType CodeType { get; set; }
        public MarkupBuilder Documentation { get; set; }
        public IGettableAssigner Assigner { get; set; }
        public bool Ambiguous { get; set; } = true;
        public bool RequiresCapture => false;
        public IVariable Provider => this;
        public CompletionItemKind CompletionItemKind { get; set; } = CompletionItemKind.Property;

        public InternalVar(string name)
        {
            Name = name;
        }

        public InternalVar(string name, CompletionItemKind kind)
        {
            Name = name;
            CompletionItemKind = kind;
        }

        public InternalVar(string name, CodeType type, CompletionItemKind kind)
        {
            Name = name;
            CodeType = type;
            CompletionItemKind = kind;
        }

        // public void Call(ParseInfo parseInfo, DocRange callRange)
        // {
        //     if (DefinedAt != null) parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
        //     parseInfo.Script.AddHover(callRange, GetLabel(true));
        //     parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new Location(parseInfo.Script.Uri, callRange));
        // }

        public CompletionItem GetCompletion() => IVariableInstance.GetCompletion(this, CompletionItemKind);

        public string GetLabel(bool markdown)
        {
            string typeName = "define";
            if (CodeType != null) typeName = CodeType.GetName();
            if (markdown) return HoverHandler.Sectioned(typeName + " " + Name, Documentation?.ToString(true));
            else return typeName + " " + Name;
        }

        public IVariableInstance GetInstance(InstanceAnonymousTypeLinker genericsLinker) => this;
        public IVariableInstance GetDefaultInstance() => this;
        public IScopeable AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker)
        {
            scopeHandler.Add(this, Static);
            return this;
        }
        public void AddDefaultInstance(IScopeAppender scopeAppender) => scopeAppender.Add(this, Static);
        public IGettableAssigner GetAssigner() => Assigner;
        public bool CanBeAmbiguous() => Ambiguous;
    }
}