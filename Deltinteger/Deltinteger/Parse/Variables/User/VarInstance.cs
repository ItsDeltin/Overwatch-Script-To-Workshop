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

        private readonly Var _var;

        public VariableInstance(Var var, InstanceAnonymousTypeLinker instanceInfo)
        {
            _var = var;
            CodeType = _var.CodeType.GetRealType(instanceInfo);
        }

        public CompletionItem GetCompletion() => _var.GetCompletion();
        public IGettableAssigner GetAssigner() => CodeType.GetGettableAssigner(_var);
    }
}