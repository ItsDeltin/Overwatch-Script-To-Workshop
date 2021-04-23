using System;
using Deltin.Deltinteger.Compiler;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class MissingElementAction : IExpression, IStatement
    {
        private readonly CodeType _type;

        public MissingElementAction(DeltinScript deltinScript)
        {
            _type = deltinScript.Types.Any();
        }

        private MissingElementAction() { }

        public Scope ReturningScope() => null;
        public CodeType Type() => _type;
        public bool IsStatement() => true;
        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment) { }
        public IWorkshopTree Parse(ActionSet actionSet) => throw new NotImplementedException();
        public void Translate(ActionSet actionSet) => throw new NotImplementedException();
    }

    public class MissingVariable : IVariable, IVariableInstance, IExpression
    {
        public string Name { get; }
        public MarkupBuilder Documentation => null;
        public bool WholeContext => true;
        public LanguageServer.Location DefinedAt => null;
        public AccessLevel AccessLevel => AccessLevel.Public;
        public CodeType CodeType { get; }
        public VariableType VariableType => VariableType.Dynamic;
        public IVariable Provider => this;
        ICodeTypeSolver IScopeable.CodeType => CodeType;

        public MissingVariable(DeltinScript deltinScript, string name)
        {
            Name = name;
            CodeType = deltinScript.Types.Any();
        }

        public bool IsStatement() => true;
        public Scope ReturningScope() => null;
        public CodeType Type() => CodeType;
        public IWorkshopTree Parse(ActionSet actionSet) => throw new NotImplementedException();
        public IVariableInstance GetInstance(InstanceAnonymousTypeLinker genericsLinker) => this;
        public IVariableInstance GetDefaultInstance() => this;
        public IGettableAssigner GetAssigner(ActionSet actionSet) => throw new NotImplementedException();
        public IScopeable AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker) => throw new NotImplementedException();
        public void AddDefaultInstance(IScopeAppender scopeAppender) => throw new NotImplementedException();
        public IVariableInstanceAttributes Attributes { get; } = new VariableInstanceAttributes();
    }
}