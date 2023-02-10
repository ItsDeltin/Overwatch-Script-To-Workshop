using Deltin.Deltinteger.LanguageServer;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    static class VariableMaker
    {
        public static IVariable New(string name, CodeType type) =>
            new GenericVariableProvider(name, type, VariableType.Dynamic, false);

        public static IVariable NewStatic(string name, CodeType type) =>
            new GenericVariableProvider(name, type, VariableType.Dynamic, true);

        public static IVariable NewPropertyLike(string name, CodeType type) =>
            new GenericVariableProvider(name, type, VariableType.ElementReference, false);

        public static IVariable NewUnambiguousPropertyLike(string name, CodeType type) =>
            new GenericVariableProvider(name, type, VariableType.ElementReference, false)
            {
                CanBeAmbiguous = false
            };

        class GenericVariableProvider : IVariable
        {
            public string Name { get; }
            public VariableType VariableType { get; }
            readonly CodeType type;
            readonly bool isStatic;
            readonly MarkupBuilder documentation = new MarkupBuilder();
            public bool CanBeAmbiguous { get; init; }

            public GenericVariableProvider(string name, CodeType type, VariableType variableType, bool isStatic)
            {
                Name = name;
                VariableType = variableType;
                this.type = type;
                this.isStatic = isStatic;
            }

            public IScopeable AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker)
            {
                var instance = GetInstance(scopeHandler.DefinedIn(), genericsLinker);
                scopeHandler.Add(instance, isStatic);
                return instance;
            }

            public IVariableInstance GetDefaultInstance(CodeType definedIn) =>
                GetInstance(definedIn, InstanceAnonymousTypeLinker.Empty);

            public IVariableInstance GetInstance(CodeType definedIn, InstanceAnonymousTypeLinker genericsLinker) =>
                new GenericVariableInstance(this, definedIn, type.GetRealType(genericsLinker));

            class GenericVariableInstance : IVariableInstance,
                IAmbiguityCheck // Note: IAmbiguityCheck should be redesigned.
            {
                public string Name => provider.Name;
                public IVariable Provider => provider;
                public MarkupBuilder Documentation => provider.documentation;
                public IVariableInstanceAttributes Attributes { get; }
                public ICodeTypeSolver CodeType => type;
                public bool WholeContext { get; } = true; // ?
                public Location DefinedAt { get; } = null;
                public AccessLevel AccessLevel { get; } = AccessLevel.Public;

                readonly GenericVariableProvider provider;
                readonly CodeType type;

                public GenericVariableInstance(GenericVariableProvider provider, CodeType definedIn, CodeType type)
                {
                    this.provider = provider;
                    this.type = type;

                    var attributes = new VariableInstanceAttributes();
                    attributes.ContainingType = definedIn;
                    Attributes = attributes;
                }

                public IGettableAssigner GetAssigner(GetVariablesAssigner getAssigner = default) => type.GetGettableAssigner(new AssigningAttributes());

                public bool CanBeAmbiguous() => provider.CanBeAmbiguous;
            }
        }
    }

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

        readonly StoreType storeType = StoreType.None;

        public InternalVar(string name, CodeType type, StoreType storeType = StoreType.None)
        {
            Name = name;
            CodeType = type;
            this.storeType = storeType;
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
        public IGettableAssigner GetAssigner(GetVariablesAssigner getAssigner) => CodeType.GetGettableAssigner(new AssigningAttributes()
        {
            Name = (getAssigner.Tag ?? string.Empty) + Name,
            IsGlobal = getAssigner.IsGlobal,
            StoreType = storeType,
            ID = -1,
            Extended = false
        });

        public bool CanBeAmbiguous() => Ambiguous;
    }
}