using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class VariableInstance : IVariableInstance
    {
        public string Name => _var.Name;
        public CodeType CodeType { get; }
        public bool WholeContext => _var.WholeContext;
        public LanguageServer.Location DefinedAt => _var.DefinedAt;
        public AccessLevel AccessLevel => _var.AccessLevel;
        IVariable IVariableInstance.Provider => _var;
        public MarkupBuilder Documentation { get; set; }
        ICodeTypeSolver IScopeable.CodeType => CodeType;
        public IVariableInstanceAttributes Attributes { get; }

        private readonly Var _var;

        public VariableInstance(Var var, InstanceAnonymousTypeLinker instanceInfo)
        {
            _var = var;
            CodeType = var.CodeType.GetRealType(instanceInfo);
            Attributes = new VariableInstanceAttributes()
            {
                CanBeSet = var.StoreType != StoreType.None,
                StoreType = var.StoreType
            };
        }

        public CompletionItem GetCompletion() => _var.GetCompletion();
        public IGettableAssigner GetAssigner(ActionSet actionSet) => CodeType.GetRealType(actionSet?.ThisTypeLinker).GetGettableAssigner(new AssigningAttributes() {
            Name = _var.Name,
            Extended = _var.InExtendedCollection,
            ID = _var.ID,
            IsGlobal = actionSet?.IsGlobal ?? true,
            StoreType = _var.StoreType,
            VariableType = _var.VariableType,
            DefaultValue = _var.InitialValue
        });
    }
}