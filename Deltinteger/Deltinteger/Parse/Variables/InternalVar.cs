using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class InternalVar : IVariable, IVariableInstance, IAmbiguityCheck
    {
        public string Name { get; }
        public VariableType VariableType { get; set; }
        public bool Static { get; set; }
        public bool WholeContext { get; set; } = true;
        public LanguageServer.Location DefinedAt { get; set; }
        public AccessLevel AccessLevel { get; set; } = AccessLevel.Public;
        public MarkupBuilder Documentation { get; set; }
        public bool Ambiguous { get; set; } = true;
        public bool RequiresCapture => false;
        public IVariable Provider => this;
        public CompletionItemKind CompletionItemKind { get; set; } = CompletionItemKind.Property;
        public IVariableInstanceAttributes Attributes { get; set; } = new VariableInstanceAttributes();
        public CodeType CodeType { get; set; }
        
        ICodeTypeSolver IScopeable.CodeType => CodeType;

        public InternalVar(string name, CodeType type)
        {
            Name = name;
            CodeType = type;
        }

        public InternalVar(string name, CodeType type, CompletionItemKind kind)
        {
            Name = name;
            CodeType = type;
            CompletionItemKind = kind;
        }

        public virtual IWorkshopTree ToWorkshop(ActionSet actionSet) => actionSet.IndexAssigner.Get(Provider).GetVariable();

        // todo
        // public void Call(ParseInfo parseInfo, DocRange callRange)
        // {
        //     if (DefinedAt != null) parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
        //     parseInfo.Script.AddHover(callRange, GetLabel(true));
        //     parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new Location(parseInfo.Script.Uri, callRange));
        // }

        public IVariableInstance GetInstance(CodeType definedIn, InstanceAnonymousTypeLinker genericsLinker) => this;
        public IVariableInstance GetDefaultInstance(CodeType definedIn) => this;
        public IScopeable AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker)
        {
            scopeHandler.Add(this, Static);
            return this;
        }
        public void AddDefaultInstance(IScopeAppender scopeAppender) => scopeAppender.Add(this, Static);
        public IGettableAssigner GetAssigner(GetVariablesAssigner getAssigner) => CodeType.GetGettableAssigner(new AssigningAttributes());
        public bool CanBeAmbiguous() => Ambiguous;
    }

    class InternalVarValue : InternalVar
    {
        readonly IWorkshopTree _value;

        public InternalVarValue(string name, CodeType type, IWorkshopTree value, CompletionItemKind kind) : base(name, type, kind) =>
            _value = value;
        
        public override IWorkshopTree ToWorkshop(ActionSet actionSet) => _value;
    }
}